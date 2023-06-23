using Pustok.Models;

namespace Pustok.ViewModels
{
    public class BookDetailViewModel
    {
        public Book Book { get; set; }
        public List<Book> RelatedBooks { get; set; }
        public BookReview BookReview { get; set; }
        public int AvgRate { get; set; }
        public bool ReviewAlowed { get; set; }
    }
}
