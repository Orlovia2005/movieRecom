namespace movieRecom.Models.DTO
{
    public class WishlistItemDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int? ReleaseYear { get; set; }
        public List<string> Genres { get; set; } = new();
        public DateTime AddedAt { get; set; }
    }

    public class WishlistDto
    {
        public List<WishlistItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class AddToWishlistDto
    {
        public int MovieId { get; set; }
    }
}
