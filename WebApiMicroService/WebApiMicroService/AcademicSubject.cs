using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiMicroService
{
    [Table("academic_subjects")]
    public class AcademicSubject
    {
        [Key]
        [Required]
        public int id { get; set; }
        [Required]
        public string name { get; set; }
    }
}