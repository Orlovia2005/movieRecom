namespace movieRecom.Models.DTO
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "User";
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "User";
        public int RatingsCount { get; set; }
        public int WishlistCount { get; set; }
        public int CommentsCount { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}
