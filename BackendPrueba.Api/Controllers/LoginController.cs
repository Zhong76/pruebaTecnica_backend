using BackendPrueba.Api.Data;
using BackendPrueba.Api.DTO;
using BackendPrueba.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackendPrueba.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly DbConnectionFactory _db;
        private readonly IConfiguration _config;

        public LoginController(DbConnectionFactory db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] D_Login request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.username) ||
                    string.IsNullOrWhiteSpace(request._password))
                {
                    return Ok(new
                    {
                        ok = false,
                        message = "Campos requeridos vacíos",
                        rowsAffected = 0,
                        data = (object?)null
                    });
                }
                using var conn = await _db.CreateOpenConnectionAsync();
                using var cmd = new SqlCommand("loginUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@username", request.username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        ok = false,
                        message = "Credenciales inválidas",
                        rowsAffected = 0,
                        data = (object?)null
                    });
                }
                var hash = reader.GetString(reader.GetOrdinal("_password"));
                var valid = BCrypt.Net.BCrypt.Verify(request._password, hash);
                if (!valid)
                {
                    return Ok(new
                    {
                        ok = false,
                        message = "Credenciales inválidas",
                        rowsAffected = 0,
                        data = (object?)null
                    });
                }
                var status = reader.GetBoolean(reader.GetOrdinal("_status"));
                if (!status)
                {
                    return Ok(new
                    {
                        ok = false,
                        message = "Usuario desactivado",
                        rowsAffected = 0,
                        data = (object?)null
                    });
                }
                var id = reader.GetInt32(reader.GetOrdinal("id"));
                var username = reader.GetString(reader.GetOrdinal("username"));
                var email = reader.GetString(reader.GetOrdinal("email"));
                var token = GenerateToken(id, username, email);
                var data = new D_LoginDTO
                {
                    id = id,
                    username = username,
                    email = email,
                    _status = status,
                    token = token
                };

                return Ok(new
                {
                    ok = true,
                    message = "Sesión iniciada correctamente.",
                    rowsAffected = 1,
                    data = data
                });
            }
            catch (SqlException ex)
            {
                return Ok(new
                {
                    ok = false,
                    message = "Error SQL: " + ex.Message,
                    rowsAffected = 0,
                    data = (object?)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    ok = false,
                    message = "Error general: " + ex.Message,
                    rowsAffected = 0,
                    data = (object?)null
                });
            }
        }

        private string GenerateToken(int id, string username, string email)
        {
            var jwt = _config.GetSection("Jwt");

            var key = jwt["Key"]!;
            var issuer = jwt["Issuer"]!;
            var audience = jwt["Audience"]!;
            var expMinutes = int.Parse(jwt["ExpireMinutes"]!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim("username", username),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<IActionResult> CreateUser(UsuarioCreateRequest request)
        {
            try
            {
                if (request is null ||
                    string.IsNullOrWhiteSpace(request.username) ||
                    string.IsNullOrWhiteSpace(request.email) ||
                    string.IsNullOrWhiteSpace(request._password))
                {
                    return Ok(new { ok = false, message = "Datos incompletos", rowsAffected = 0 });
                }

                using var conn = await _db.CreateOpenConnectionAsync();
                using (var checkCmd = new SqlCommand("listarUsuario", conn))
                {
                    checkCmd.CommandType = CommandType.StoredProcedure;
                    checkCmd.Parameters.AddWithValue("@id", DBNull.Value);
                    checkCmd.Parameters.AddWithValue("@username", request.username!.Trim());

                    using var readerCheck = await checkCmd.ExecuteReaderAsync();
                    if (await readerCheck.ReadAsync())
                    {
                        return Ok(new { ok = false, message = "Usuario existente", rowsAffected = 0, id = (int?)null });
                    }
                }

                var passHash = BCrypt.Net.BCrypt.HashPassword(request._password);

                using var cmd = new SqlCommand("crearUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@username", request.username.Trim());
                cmd.Parameters.AddWithValue("@email", request.email.Trim());
                cmd.Parameters.AddWithValue("@passHash", passHash);
                cmd.Parameters.AddWithValue("@_status", request._status);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return Ok(new { ok = false, message = "No se obtuvo respuesta del procedimiento", rowsAffected = 0, id = (int?)null });

                return Ok(new
                {
                    ok = reader.GetBoolean(reader.GetOrdinal("ok")),
                    message = reader.GetString(reader.GetOrdinal("message")),
                    rowsAffected = 1,
                    id = reader.GetInt32(reader.GetOrdinal("id"))
                });
            }
            catch (Exception ex)
            {
                return Ok(new { ok = false, message = ex.Message, rowsAffected = 0, id = (int?)null });
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public Task<IActionResult> Register(UsuarioCreateRequest request) => CreateUser(request);

        [AllowAnonymous]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (request is null ||
                    string.IsNullOrWhiteSpace(request.username) ||
                    string.IsNullOrWhiteSpace(request.oldPassword) ||
                    string.IsNullOrWhiteSpace(request.newPassword))
                {
                    return Ok(new { ok = false, message = "Datos incompletos", rowsAffected = 0 });
                }

                using var conn = await _db.CreateOpenConnectionAsync();

                int id;
                bool status;
                string passHashDb;

                using (var cmdLogin = new SqlCommand("loginUsuario", conn))
                {
                    cmdLogin.CommandType = CommandType.StoredProcedure;
                    cmdLogin.Parameters.AddWithValue("@username", request.username.Trim());

                    using var reader = await cmdLogin.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return Ok(new { ok = false, message = "Usuario no existe", rowsAffected = 0 });

                    status = reader.GetBoolean(reader.GetOrdinal("_status"));
                    if (!status)
                        return Ok(new { ok = false, message = "Usuario desactivado", rowsAffected = 0 });

                    id = reader.GetInt32(reader.GetOrdinal("id"));
                    passHashDb = reader.GetString(reader.GetOrdinal("_password"));
                }

                var oldMatch = BCrypt.Net.BCrypt.Verify(request.oldPassword, passHashDb);
                if (!oldMatch)
                    return Ok(new { ok = false, message = "La contraseña antigua no coincide", rowsAffected = 0 });

                var newHash = BCrypt.Net.BCrypt.HashPassword(request.newPassword);

                using var cmdUpd = new SqlCommand("editarUsuario", conn);
                cmdUpd.CommandType = CommandType.StoredProcedure;
                cmdUpd.Parameters.AddWithValue("@id", id);
                cmdUpd.Parameters.AddWithValue("@username", request.username!);
                cmdUpd.Parameters.AddWithValue("@email", DBNull.Value);
                cmdUpd.Parameters.AddWithValue("@passHash", newHash);
                cmdUpd.Parameters.AddWithValue("@_status", DBNull.Value);

                using var readerUpd = await cmdUpd.ExecuteReaderAsync();
                if (!await readerUpd.ReadAsync())
                    return Ok(new { ok = false, message = "No se obtuvo respuesta del procedimiento", rowsAffected = 0 });

                return Ok(new
                {
                    ok = readerUpd.GetBoolean(readerUpd.GetOrdinal("ok")),
                    message = readerUpd.GetString(readerUpd.GetOrdinal("message")),
                    rowsAffected = readerUpd.GetInt32(readerUpd.GetOrdinal("rowsAffected"))
                });
            }
            catch (SqlException ex)
            {
                return Ok(new { ok = false, message = "Error SQL: " + ex.Message, rowsAffected = 0 });
            }
            catch (Exception ex)
            {
                return Ok(new { ok = false, message = "Error general: " + ex.Message, rowsAffected = 0 });
            }
        }
    }
}
