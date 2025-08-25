using iText.Kernel.Pdf;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
namespace Server.Models
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string? FileName { get; set; }

        public Byte[]? FileData { get; set; }

        public bool Completed { get; set; }

        [AllowNull]
        public DateTime? CompletionDate { get; set; }

        public User? Owner { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        [Precision(0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastModified { get; set; }

        public List<Material>? MaterialList { get; set; }

        public PdfDocument? GetFileFromData()
        {
            if (FileData == null) return null;

            using var ms = new MemoryStream(FileData);
            var pdfReader = new PdfReader(ms);
            return new PdfDocument(pdfReader);
        }
    }
}
