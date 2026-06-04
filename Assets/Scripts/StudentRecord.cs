using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("students")]
public class StudentRecord : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; }

    [Column("student_code")]
    public string StudentCode { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("image_path")]
    public string ImagePath { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }
}