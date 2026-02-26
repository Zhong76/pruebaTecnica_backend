using BackendPrueba.Api.Data;
using BackendPrueba.Api.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using BackendPrueba.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace BackendPrueba.Api.Controllers
{
    [Route("api/usuario")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly DbConnectionFactory _db;
        public UsuarioController(DbConnectionFactory db)
        {
            _db = db;
        }
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<D_UsuarioDTO>>> Get([FromQuery] int? id, [FromQuery] string? username)
        {
            try
            {
                var list = new List<D_UsuarioDTO>();

                using var conn = await _db.CreateOpenConnectionAsync();
                using var cmd = new SqlCommand("listarUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", (object?)id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@username", (object?)username ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new D_UsuarioDTO
                    {
                        id = reader.GetInt32(reader.GetOrdinal("id")),
                        username = reader.GetString(reader.GetOrdinal("username")),
                        email = reader.GetString(reader.GetOrdinal("email")),
                        _status = reader.GetBoolean(reader.GetOrdinal("_status")),
                        createdAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        updatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                    });
                }

                return Ok(new
                {
                    ok = true,
                    message = "OK",
                    rowsAffected = list.Count,
                    data = list
                });
            }
            catch (SqlException ex)
            {
                return Ok(new { ok = false, message = "Error SQL: " + ex.Message, rowsAffected = 0, data = (object?)null });
            }
            catch (Exception ex)
            {
                return Ok(new { ok = false, message = "Error general: " + ex.Message, rowsAffected = 0, data = (object?)null });
            }
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<D_UsuarioDTO>> GetById(int id)
        {
            try
            {
                using var conn = await _db.CreateOpenConnectionAsync();
                using var cmd = new SqlCommand("listarUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@username", DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        ok = false,
                        message = "Usuario no encontrado",
                        rowsAffected = 0,
                        data = (object?)null
                    });
                }

                var user = new D_UsuarioDTO
                {
                    id = reader.GetInt32(reader.GetOrdinal("id")),
                    username = reader.GetString(reader.GetOrdinal("username")),
                    email = reader.GetString(reader.GetOrdinal("email")),
                    _status = reader.GetBoolean(reader.GetOrdinal("_status")),
                    createdAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    updatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };

                return Ok(new
                {
                    ok = true,
                    message = "OK",
                    rowsAffected = 1,
                    data = user
                });
            }
            catch (SqlException ex)
            {
                return Ok(new { ok = false, message = "Error SQL: " + ex.Message, rowsAffected = 0, data = (object?)null });
            }
            catch (Exception ex)
            {
                return Ok(new { ok = false, message = "Error general: " + ex.Message, rowsAffected = 0, data = (object?)null });
            }
        }

        [Authorize]
        [HttpPost]
        public Task<IActionResult> Create(UsuarioCreateRequest request) => CreateUser(request, isPublic: false);

        private async Task<IActionResult> CreateUser(UsuarioCreateRequest request, bool isPublic)
        {
            throw new NotImplementedException();
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UsuarioUpdateRequest request)
        {
            try
            {
                if (request is null)
                    return Ok(new { ok = false, message = "Body requerido", rowsAffected = 0 });

                if (string.IsNullOrWhiteSpace(request.username) || string.IsNullOrWhiteSpace(request.email) || request._status is null)
                    return Ok(new { ok = false, message = "username, email y _status son requeridos", rowsAffected = 0 });

                string? passHash = null;
                if (!string.IsNullOrWhiteSpace(request._password))
                    passHash = BCrypt.Net.BCrypt.HashPassword(request._password);

                using var conn = await _db.CreateOpenConnectionAsync();
                using var cmd = new SqlCommand("editarUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@username", request.username!);
                cmd.Parameters.AddWithValue("@email", request.email!);
                cmd.Parameters.AddWithValue("@passHash", (object?)passHash ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@_status", request._status.Value);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return Ok(new { ok = false, message = "No se obtuvo respuesta del procedimiento", rowsAffected = 0 });

                return Ok(new
                {
                    ok = reader.GetBoolean(reader.GetOrdinal("ok")),
                    message = reader.GetString(reader.GetOrdinal("message")),
                    rowsAffected = reader.GetInt32(reader.GetOrdinal("rowsAffected"))
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

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var conn = await _db.CreateOpenConnectionAsync();
                using var cmd = new SqlCommand("desactivarUsuario", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        ok = false,
                        message = "No se obtuvo respuesta del procedimiento",
                        rowsAffected = 0
                    });
                }

                return Ok(new
                {
                    ok = reader.GetBoolean(reader.GetOrdinal("ok")),
                    message = reader.GetString(reader.GetOrdinal("message")),
                    rowsAffected = reader.GetInt32(reader.GetOrdinal("rowsAffected"))
                });
            }
            catch (SqlException ex)
            {
                return Ok(new
                {
                    ok = false,
                    message = "Error SQL: " + ex.Message,
                    rowsAffected = 0
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    ok = false,
                    message = "Error general: " + ex.Message,
                    rowsAffected = 0
                });
            }
        }
    }
}
