namespace movieRecom.Models.Model
{
    public class User
    {
        public User()
        {
        }

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        
        public UserRole Role { get; set; } = UserRole.User;

        // Navigation properties
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Wishlist> WishlistItems { get; set; } = new List<Wishlist>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}

