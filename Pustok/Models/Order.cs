using Pustok.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pustok.Models
{
	public class Order
	{
		public int Id { get; set; }
		public string AppUserId { get; set; }
		[Required]
		[MaxLength(35)]
		public string FullName { get; set; }
		[Required]
		[MaxLength(25)]
		public string City { get; set; }
		[Required]
		[MaxLength(250)]
		public string Address { get; set; }
		[Required]
		[MaxLength(100)]
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }
		[Required]
		[MaxLength(35)]
		public string Phone { get; set; }
		[Required]
		[MaxLength(10)]

		public string ZipCode { get; set; }
		[MaxLength(500)]
		public string Note { get; set; }
		public DateTime CreatedAt { get; set; }
		[Column(TypeName = "tinyint")]
		public OrderStatus Status { get; set; }
		public AppUser User { get; set; }

		public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
	}
}
