namespace HomeAssignment.DTOs
{
    public class DataItemDto
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDataItemDto
    {
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateDataItemDto
    {
        public string Value { get; set; } = string.Empty;
    }
}
