using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiMicroService
{
    [Table("user")]
    public class User
    {
        [Key]
        [Required]
        public int id { get; set; }
        [Required]
        public string login { get; set; }
        public string? password { get; set; }
        [Required]
        public string firstname{ get; set; }

        public string? middlename { get; set; }
        [Required]
        public string lastname { get; set; }

        public byte[]? photo { get; set; }
        public string? about_me { get; set; } 
        public string? phone_number { get; set; }
        [Required]
        public string email { get; set; } 
        public string? telegram_nick { get; set; }
        [Required]
        public bool hide_contacts { get; set; } 


    }
}