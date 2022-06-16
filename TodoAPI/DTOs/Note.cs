namespace TodoAPI.DTOs
{
    record Note(int id){
    public string text {get;set;} = default!;
    public string name { get; set; } = default!;
    public Status status { get; set; }
    public int userId { get; set; } = default!;
    public DateTime created { get; set; } = DateTime.UtcNow;

    public DateTime updated { get; set; } = default!;
}
}