namespace BackendPrueba.Api.Models
{
    public class D_Usuario
    {
    }
    public class UsuarioCreateRequest
    {
        public string? username { get; set; }
        public string? email { get; set; }
        public string? _password { get; set; }
        public Boolean? _status { get; set; }
    }
    public class UsuarioUpdateRequest
    {
        public string? username { get; set; }
        public string? email { get; set; }
        public string? _password { get; set; }
        public Boolean? _status { get; set; }
    }
    public class ChangePasswordRequest
    {
        public string? username { get; set; }
        public string? oldPassword { get; set; }
        public string? newPassword { get; set; }
    }
}
