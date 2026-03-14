using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abstracciones.Interfaces.DA;
using Abstracciones.Modelos.Evidence;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DA
{
    public class EvidenceDA : IEvidenceDA
    {
        private readonly IRepositorioDapper _repositorioDapper;
        private readonly SqlConnection _sqlConnection;

        public EvidenceDA(IRepositorioDapper repositorioDapper)
        {
            _repositorioDapper = repositorioDapper;
            _sqlConnection = _repositorioDapper.ObtenerRepositorio();
        }

        public async Task<int> SaveEvidenceAsync(int taskId, int empleadoId, string fileName, string filePath, string? notes)
        {
            // First, check if the task exists in Ciclo_Checklist
            const string checkSql = @"
                SELECT ciclo_checklist_id 
                FROM dbo.Ciclo_Checklist 
                WHERE tarea_id = @TaskId 
                  AND completado = 1 
                  AND completado_por = @EmpleadoId
                ORDER BY fecha_completado DESC";

            var cicloChecklistId = await _sqlConnection.QueryFirstOrDefaultAsync<long?>(
                checkSql,
                new { TaskId = taskId, EmpleadoId = empleadoId }
            );

            // If no completion record found, create one
            if (!cicloChecklistId.HasValue)
            {
                const string insertCompleteSql = @"
                    DECLARE @ciclo_id INT = (
                        SELECT TOP 1 ciclo_id 
                        FROM dbo.Ciclos 
                        WHERE responsable_id = @EmpleadoId 
                          AND fecha_cosecha_real IS NULL
                        ORDER BY ciclo_id
                    );
                    
                    IF @ciclo_id IS NOT NULL
                    BEGIN
                        INSERT INTO dbo.Ciclo_Checklist (ciclo_id, tarea_id, completado, completado_por, fecha_completado, fecha_creacion)
                        VALUES (@ciclo_id, @TaskId, 1, @EmpleadoId, SYSDATETIME(), SYSDATETIME());
                        
                        SELECT SCOPE_IDENTITY() AS ciclo_checklist_id;
                    END
                    ELSE
                    BEGIN
                        SELECT NULL AS ciclo_checklist_id;
                    END";

                cicloChecklistId = await _sqlConnection.ExecuteScalarAsync<long?>(
                    insertCompleteSql,
                    new { TaskId = taskId, EmpleadoId = empleadoId }
                );
            }

            if (!cicloChecklistId.HasValue || cicloChecklistId.Value == 0)
                throw new Exception("No se pudo encontrar o crear un registro de checklist para esta tarea.");

            // Insert evidence
            const string sp = "dbo.sp_Evidencias_Insert";

            var parameters = new
            {
                ciclo_checklist_id = cicloChecklistId.Value,
                nombre_archivo = fileName,
                ruta_archivo = filePath,
                tipo_contenido = "image/jpeg", // You might want to detect this from the file
                tamano_bytes = 0, // You'd get this from the file
                notas = notes,
                subido_por = empleadoId
            };

            var id = await _sqlConnection.ExecuteScalarAsync<int>(
                sp,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return id;
        }
    }
}