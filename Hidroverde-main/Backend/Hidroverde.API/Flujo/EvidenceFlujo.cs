using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abstracciones.Interfaces.DA;
using Abstracciones.Interfaces.Flujo;
using Abstracciones.Modelos.Evidence;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Flujo
{
    public class EvidenceFlujo : IEvidenceFlujo
    {
        private readonly IEvidenceDA _evidenceDA;

        public EvidenceFlujo(IEvidenceDA evidenceDA)
        {
            _evidenceDA = evidenceDA;
        }

        public async Task<EvidenceUploadResponse> UploadEvidenceAsync(int taskId, int empleadoId, IFormFile file, string? notes)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("File type not allowed. Allowed types: jpg, jpeg, png, pdf, docx");

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 10MB limit.");

            // Generate unique filename
            string fileName = $"{Guid.NewGuid()}{extension}";

            // In production, you would save to disk/cloud here
            // For now, we'll just store the path in the database
            string filePath = $"/uploads/evidence/{fileName}";

            // Save metadata via DA
            int evidenceId = await _evidenceDA.SaveEvidenceAsync(taskId, empleadoId, fileName, filePath, notes);

            return new EvidenceUploadResponse
            {
                EvidenceId = evidenceId,
                FileName = fileName,
                UploadedAt = DateTime.Now,
                Message = "Evidence uploaded successfully."
            };
        }
    }
}