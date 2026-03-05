# Claude Code + Get Shit Done Setup

## Что было настроено

### 1. Библиотека Get Shit Done (GSD)
- GSD уже установлена в `~/.claude/get-shit-done/`
- Все шаблоны и workflow доступны
- Конфигурация находится в `~/.claude/get-shit-done/templates/config.json`

### 2. Настройки разрешений
Файл `~/.claude/settings.json` обновлен:
```json
{
  "permissions": {
    "allow": ["Bash", "Read", "Write", "Edit", "Grep", "Glob"],
    "defaultMode": "bypassPermissions"
  },
  "model": "sonnet"
}
```

**Важно**: Режим `bypassPermissions` пропускает все запросы разрешений автоматически.

### 3. Команда claude-skip
Создан скрипт `~/.local/bin/claude-skip` для запуска с явным пропуском разрешений.

## Использование

### Обычный запуск Claude
```bash
claude
```

### Запуск с пропуском разрешений
```bash
claude-skip
# или
claude-gsd -skip
```

### Доступные GSD команды (skills)

В Claude Code используйте команды через `/` или через Skill tool:

- `/gsd:progress` - Проверить прогресс проекта
- `/gsd:help` - Показать все доступные команды GSD
- `/gsd:new-project` - Инициализировать новый проект
- `/gsd:create-roadmap` - Создать roadmap с фазами
- `/gsd:plan-phase` - Создать детальный план для фазы
- `/gsd:execute-phase` - Выполнить все планы в фазе
- `/gsd:execute-plan` - Выполнить PLAN.md файл
- `/gsd:map-codebase` - Анализ codebase
- `/gsd:add-todo` - Добавить задачу
- `/gsd:check-todos` - Показать pending todos
- `/gsd:verify-work` - UAT тестирование
- `/gsd:resume-work` - Возобновить работу из предыдущей сессии

### Пример использования GSD

1. Инициализация проекта:
   ```
   /gsd:new-project
   ```

2. Создание roadmap:
   ```
   /gsd:create-roadmap
   ```

3. Планирование фазы:
   ```
   /gsd:plan-phase
   ```

4. Выполнение:
   ```
   /gsd:execute-phase
   ```

## Полезная информация

- **Проекты хранятся в**: `~/.claude/projects/`
- **Планы хранятся в**: `~/.claude/plans/`
- **История**: `~/.claude/history.jsonl`
- **Конфигурация GSD**: `~/.claude/get-shit-done/`

## Безопасность

⚠️ **Внимание**: Режим `bypassPermissions` автоматически разрешает все операции без запросов.
Используйте с осторожностью!

Если хотите вернуть запросы разрешений, измените в `~/.claude/settings.json`:
```json
"defaultMode": "default"
```

## Обновление настроек

После изменения `.zshrc` выполните:
```bash
source ~/.zshrc
```

## Проверка работы

```bash
# Проверить версию Claude
claude --version

# Проверить, что claude-skip доступен
which claude-skip

# Запустить GSD help
# В Claude Code сессии: /gsd:help
```

---
Настроено: 2026-03-05
