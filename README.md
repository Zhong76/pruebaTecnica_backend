#Backend
## Tecnologías utilizadas
- .NET .NET 10.0.103
- ASP.NET Core
- SQL Server
- JWT Authentication
- BCrypt
- Swagger

## Instalación
Ubicarse en el directorio donde se desea descargar el proyecto y ejecutar:
```bash
###Clonar repositorio:
-- git clone https://github.com/Zhong76/pruebaTecnica_backend.git
###Ubicarse en carpeta:
-- cd BackendPrueba.Api
###Restaurar dependencias:
-- dotnet restore
###Configurar en appsettings.json:
-- ConnectionStrings (conexión a bd)
-- Jwt Key (key para generación de token)
###Ejecución
-- dotnet run

##Abrir Swagger en el navegador:
https://localhost:XXXX/swagger -- En la consola se mostrará el puerto. Ej: https://localhost:7165, se debe agregar /swagger en el navegador, quedando de esta forma: https://localhost:7165/swagger.
--El puerto se debe guardar para colocarlo en el frontend.
