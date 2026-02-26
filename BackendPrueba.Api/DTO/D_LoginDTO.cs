namespace BackendPrueba.Api.DTO
{
    public class D_LoginDTO
    {
        public int? id { get; set; }
        public string? username { get; set; }
        public string? email { get; set; }
        public Boolean? _status { get; set; }
        public string? token { get; set; }
    }
}
