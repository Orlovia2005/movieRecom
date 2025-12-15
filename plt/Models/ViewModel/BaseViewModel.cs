using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace plt.Models.ViewModel
{
    /// <summary>
    /// Базовая ViewModel с реализацией INotifyPropertyChanged, IsModified и дополнительными функциями
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, object?> _originalValues = new Dictionary<string, object?>();
        private readonly HashSet<string> _modifiedProperties = new HashSet<string>();
        private bool _isModified;

        #region IsModified Implementation

        /// <summary>
        /// Флаг указывающий, что данные были изменены
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            private set => SetProperty(ref _isModified, value);
        }

        /// <summary>
        /// Список измененных свойств
        /// </summary>
        public IReadOnlyCollection<string> ModifiedProperties => _modifiedProperties.ToList().AsReadOnly();

        /// <summary>
        /// Событие изменения состояния модификации
        /// </summary>
        public event EventHandler<ModifiedStateChangedEventArgs>? ModifiedStateChanged;

        /// <summary>
        /// Начинает отслеживание изменений для свойства
        /// </summary>
        protected void TrackOriginalValue<T>(T? value, [CallerMemberName] string propertyName = "")
        {
            if (_originalValues.ContainsKey(propertyName))
            {
                _originalValues[propertyName] = value;
            }
            else
            {
                _originalValues.Add(propertyName, value);
            }

            // Убираем из измененных, если значение совпадает с оригинальным
            if (IsPropertyModified(propertyName))
            {
                _modifiedProperties.Remove(propertyName);
                UpdateModifiedState();
            }
        }

        /// <summary>
        /// Проверяет, было ли изменено конкретное свойство
        /// </summary>
        public bool IsPropertyModified(string propertyName)
        {
            if (!_originalValues.ContainsKey(propertyName))
                return false;

            var currentValue = GetType().GetProperty(propertyName)?.GetValue(this);
            var originalValue = _originalValues[propertyName];

            return !Equals(currentValue, originalValue);
        }

        /// <summary>
        /// Получает оригинальное значение свойства
        /// </summary>
        public T? GetOriginalValue<T>(string propertyName)
        {
            return _originalValues.ContainsKey(propertyName) ? (T?)_originalValues[propertyName] : default(T);
        }

        /// <summary>
        /// Обновляет состояние модификации
        /// </summary>
        private void UpdateModifiedState()
        {
            var wasModified = IsModified;
            IsModified = _modifiedProperties.Any();

            if (wasModified != IsModified)
            {
                ModifiedStateChanged?.Invoke(this, new ModifiedStateChangedEventArgs(IsModified, ModifiedProperties));
            }
        }

        /// <summary>
        /// Сбрасывает флаг изменений для всех свойств
        /// </summary>
        public virtual void AcceptChanges()
        {
            // Обновляем оригинальные значения текущими
            foreach (var propertyName in _modifiedProperties.ToList())
            {
                var currentValue = GetType().GetProperty(propertyName)?.GetValue(this);
                if (_originalValues.ContainsKey(propertyName))
                {
                    _originalValues[propertyName] = currentValue;
                }
                else
                {
                    _originalValues.Add(propertyName, currentValue);
                }
            }

            _modifiedProperties.Clear();
            UpdateModifiedState();
            ClearMessages();
        }

        /// <summary>
        /// Откатывает изменения к оригинальным значениям
        /// </summary>
        public virtual void RejectChanges()
        {
            foreach (var propertyName in _modifiedProperties.ToList())
            {
                if (_originalValues.ContainsKey(propertyName))
                {
                    var property = GetType().GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(this, _originalValues[propertyName]);
                    }
                }
            }

            _modifiedProperties.Clear();
            UpdateModifiedState();
            ClearMessages();
        }

        /// <summary>
        /// Откатывает изменения для конкретного свойства
        /// </summary>
        public void RejectPropertyChanges(string propertyName)
        {
            if (_modifiedProperties.Contains(propertyName) && _originalValues.ContainsKey(propertyName))
            {
                var property = GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(this, _originalValues[propertyName]);
                }
                _modifiedProperties.Remove(propertyName);
                UpdateModifiedState();
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "", bool trackModification = true)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            // Сохраняем оригинальное значение при первом изменении
            if (trackModification && !_modifiedProperties.Contains(propertyName) && _originalValues.ContainsKey(propertyName))
            {
                _modifiedProperties.Add(propertyName);
                UpdateModifiedState();
            }

            field = value;
            OnPropertyChanged(propertyName);

            // Валидация при изменении свойства
            if (trackModification)
            {
                ValidateProperty(propertyName);
            }

            return true;
        }

        /// <summary>
        /// Установка свойства без отслеживания изменений (для инициализации)
        /// </summary>
        protected bool SetPropertyWithoutTracking<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            return SetProperty(ref field, value, propertyName, false);
        }

        #endregion

        #region Валидация (INotifyDataErrorInfo)

        private Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        public Dictionary<string, List<string>> Errors
        {
            get => _errors;
            private set => SetProperty(ref _errors, value);
        }

        public bool HasErrors => Errors.Any();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return Enumerable.Empty<string>();

            return Errors.ContainsKey(propertyName) ? Errors[propertyName] : Enumerable.Empty<string>();
        }

        protected void AddError(string propertyName, string error)
        {
            if (!Errors.ContainsKey(propertyName))
                Errors[propertyName] = new List<string>();

            if (!Errors[propertyName].Contains(error))
            {
                Errors[propertyName].Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (Errors.ContainsKey(propertyName))
            {
                Errors.Remove(propertyName);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        protected void ClearAllErrors()
        {
            var propertiesWithErrors = Errors.Keys.ToList();
            Errors.Clear();

            foreach (var propertyName in propertiesWithErrors)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }

            OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Валидация свойства (переопределить в наследниках)
        /// </summary>
        protected virtual void ValidateProperty(string propertyName)
        {
            ClearErrors(propertyName);

            // Базовая валидация не-null для обязательных полей
            var property = GetType().GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(this);
                var attributes = property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true);

                if (attributes.Any() && (value == null || (value is string str && string.IsNullOrWhiteSpace(str))))
                {
                    AddError(propertyName, $"{propertyName} является обязательным полем");
                }
            }
        }

        #endregion

        #region Состояние загрузки и сообщения

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string? _message;
        public string? Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private MessageType _messageType;
        public MessageType MessageType
        {
            get => _messageType;
            set => SetProperty(ref _messageType, value);
        }

        public void SetMessage(string? message, MessageType type = MessageType.Info)
        {
            Message = message;
            MessageType = type;
        }

        public void ClearMessages()
        {
            Message = null;
            MessageType = MessageType.Info;
        }

        #endregion

        #region Команды (упрощенная версия без CommandManager)

        public class RelayCommand : System.Windows.Input.ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;
            private bool _isExecuting;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
            }

            public void Execute(object? parameter)
            {
                if (_isExecuting) return;

                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Async команда
        public class AsyncRelayCommand : System.Windows.Input.ICommand
        {
            private readonly Func<object?, Task> _execute;
            private readonly Func<object?, bool>? _canExecute;
            private bool _isExecuting;

            public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
            }

            public async void Execute(object? parameter)
            {
                if (_isExecuting) return;

                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    await _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Вспомогательные методы

        protected void InitializeTracking()
        {
            // Автоматически трекаем все свойства при инициализации
            var properties = GetType().GetProperties()
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var value = property.GetValue(this);
                TrackOriginalValue(value, property.Name);
            }
        }

        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        #endregion
    }

    #region Вспомогательные классы

    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ModifiedStateChangedEventArgs : EventArgs
    {
        public bool IsModified { get; }
        public IReadOnlyCollection<string> ModifiedProperties { get; }

        public ModifiedStateChangedEventArgs(bool isModified, IReadOnlyCollection<string> modifiedProperties)
        {
            IsModified = isModified;
            ModifiedProperties = modifiedProperties;
        }
    }

    #endregion
}