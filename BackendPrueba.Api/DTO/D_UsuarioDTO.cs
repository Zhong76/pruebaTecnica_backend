namespace BackendPrueba.Api.DTO
{
    public class D_UsuarioDTO
    {
        public int? id { get; set; }
        public string? username { get; set; }
        public string? email { get; set; }
        public Boolean? _status { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
    }
}
