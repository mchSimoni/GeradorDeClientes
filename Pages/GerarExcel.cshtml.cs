using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace GeradorDeClientes.Pages
{
    [Authorize]
    public class GerarExcelModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GerarExcelModel> _logger;

        public GerarExcelModel(IConfiguration config, ILogger<GerarExcelModel> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string Mensagem { get; set; } = string.Empty;
    
    public string LinkArquivo { get; set; } = string.Empty;
    public string PreviewHtml { get; set; } = string.Empty;
    public int Quantidade { get; set; } = 10;

    public IActionResult OnPost(int quantidade, string action, string delimiter = ",", string? targetEmail = null)
        {
            try
            {
                
                Quantidade = quantidade;
                
            if (quantidade < 10) quantidade = 10;
            if (quantidade > 1000) quantidade = 1000;
            
            
            if (string.Equals(action, "enviar", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var files = Directory.GetFiles(dir, "Clientes_*.xlsx");
                    if (files == null || files.Length == 0)
                    {
                        Mensagem = "Nenhum arquivo gerado previamente. Gere o arquivo antes de enviar por e-mail.";
                        return Page();
                    }

                    
                    var latest = files.Select(f => new FileInfo(f)).OrderByDescending(fi => fi.LastWriteTime).First();
                    var filePathExisting = latest.FullName;
                    var fileNameExisting = latest.Name;

                    if (string.IsNullOrWhiteSpace(targetEmail))
                    {
                        Mensagem = "Informe um e-mail de destino para envio.";
                        return Page();
                    }

                    var sent = SendEmailWithAttachment(filePathExisting, fileNameExisting, targetEmail);
                    if (sent)
                    {
                        Mensagem = $"Arquivo enviado por email com sucesso: {fileNameExisting} para {targetEmail}";
                    }
                    else
                    {
                        Mensagem = "Não foi possível enviar o e-mail. Verifique a configuração de SMTP.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar arquivo existente por email");
                    Mensagem = "Não foi possível enviar o e-mail.";
                }

                return Page();
            }

            
            var fileName = $"Clientes_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);

            var rand = new Random();
            
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Clientes");
                
                ws.Cell(1, 1).Value = "Nome";
                ws.Cell(1, 2).Value = "Email";
                ws.Cell(1, 3).Value = "Telefone";
                ws.Cell(1, 4).Value = "Data de Nascimento";
                ws.Cell(1, 5).Value = "Endereco";
                ws.Cell(1, 6).Value = "Cidade";
                ws.Cell(1, 7).Value = "Estado";
                ws.Cell(1, 8).Value = "CEP";
                ws.Cell(1, 9).Value = "CPF";

                for (int i = 0; i < quantidade; i++)
                {
                    var row = i + 2;
                    ws.Cell(row, 1).Value = $"Cliente {i + 1}";
                    ws.Cell(row, 2).Value = $"cliente{i + 1}@teste.com";
                    ws.Cell(row, 3).Value = $"(44) 9{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}";
                    ws.Cell(row, 4).Value = DateTime.Now.AddYears(-rand.Next(18, 60)).ToString("dd/MM/yyyy");
                    ws.Cell(row, 5).Value = $"Rua Exemplo {rand.Next(1, 999)}";
                    ws.Cell(row, 6).Value = "CidadeTeste";
                    ws.Cell(row, 7).Value = "SP";
                    ws.Cell(row, 8).Value = $"{rand.Next(10000, 99999)}-{rand.Next(100, 999)}";
                    ws.Cell(row, 9).Value = GenerateCpf(rand);
                }

                
                ws.Columns().AdjustToContents();
                wb.SaveAs(filePath);
            }

            LinkArquivo = fileName;

            Mensagem = $"Arquivo gerado com sucesso: {fileName}";

            
            try
            {
                using (var wb = new XLWorkbook(filePath))
                {
                    var ws = wb.Worksheet(1);
                    var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 1, 1001);
                    var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;

                    var totalRecords = Math.Max(0, (ws.LastRowUsed()?.RowNumber() ?? 1) - 1);
                    var showingRecords = Math.Max(0, lastRow - 1);

                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"<div style=\"margin-bottom:6px;font-size:0.95rem;\">Mostrando {showingRecords} de {totalRecords} registros</div>");
                    sb.AppendLine("<div style=\"overflow:auto; max-height:600px; border:1px solid #ddd; padding:8px;\">\n");
                    sb.AppendLine("<table style=\"border-collapse:collapse; width:100%;\">\n");
                    sb.AppendLine("<thead><tr>");

                    
                    for (int c = 1; c <= lastCol; c++)
                    {
                        var h = ws.Cell(1, c).GetString();
                        sb.AppendLine($"<th style=\"border:1px solid #ccc; padding:4px; text-align:left;\">{System.Net.WebUtility.HtmlEncode(h)}</th>");
                    }
                    sb.AppendLine("</tr></thead>");
                    sb.AppendLine("<tbody>");

                    for (int r = 2; r <= lastRow; r++)
                    {
                        sb.AppendLine("<tr>");
                        for (int c = 1; c <= lastCol; c++)
                        {
                            var val = ws.Cell(r, c).GetString();
                            sb.AppendLine($"<td style=\"border:1px solid #ccc; padding:4px;\">{System.Net.WebUtility.HtmlEncode(val)}</td>");
                        }
                        sb.AppendLine("</tr>");
                    }

                    sb.AppendLine("</tbody>");
                    sb.AppendLine("</table>");
                    sb.AppendLine("</div>");
                    PreviewHtml = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Erro ao gerar preview XLSX: " + ex);
            }

                return Page();
            }
            catch (Exception ex)
            {
                
                Console.Error.WriteLine(ex.ToString());
                Mensagem = "Ocorreu um erro ao gerar o arquivo: " + ex.Message;
                return Page();
            }
        }

    
    private bool SendEmailWithAttachment(string filePath, string fileName, string toEmail)
    {
        
        var smtpSection = _config.GetSection("Smtp");
        var host = smtpSection.GetValue<string>("Host");
        var port = smtpSection.GetValue<int>("Port");
        var user = smtpSection.GetValue<string>("User");
        var pass = smtpSection.GetValue<string>("Pass");
        var enableSsl = smtpSection.GetValue<bool>("EnableSsl");
        var forcePickup = smtpSection.GetValue<bool>("ForcePickup");

        bool smtpLooksValid = !string.IsNullOrEmpty(host) && host != "smtp.example.com" && port > 0 && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass);

    
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(user ?? "noreply", !string.IsNullOrEmpty(user) ? user : "noreply@example.com"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "[GeradorDeClientes] - Dados Gerados";

     var bodyText = "Segue em anexo o arquivo gerado pelo GeradorDeClientes.\n\n" +
         "Página de login: https://geradordeclientes-production.up.railway.app/Login\n\n" +
         "Repositório: https://github.com/mchSimoni/GeradorDeClientes\n\n" +
         "Boa noite, execute a aplicação no seu ambiente local que será possível efetuar login e enviar o e‑mail.\n\n" +
         "Obrigado.";
        var body = new TextPart("plain")
        {
            Text = bodyText
        };

        var multipart = new Multipart("mixed") { body };

        
        try
        {
            var attachment = new MimePart("application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                Content = new MimeContent(System.IO.File.OpenRead(filePath)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = fileName
            };
            multipart.Add(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao anexar arquivo {File}", filePath);
        }

        message.Body = multipart;

        if (!smtpLooksValid || forcePickup)
        {
            return false;
        }

        try
        {
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                SecureSocketOptions socketOptions;
                if (enableSsl)
                {
                    socketOptions = (port == 465) ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                }
                else
                {
                    socketOptions = SecureSocketOptions.None;
                }

                client.Connect(host, port, socketOptions);

                if (!string.IsNullOrEmpty(user))
                {
                    client.Authenticate(user, pass);
                }

                client.Send(message);
                client.Disconnect(true);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail via SMTP");
            return false;
        }
    }

        private string GenerateCpf(Random rand)
        {
            
            int n1 = rand.Next(100, 999);
            int n2 = rand.Next(100, 999);
            int n3 = rand.Next(100, 999);
            int n4 = rand.Next(10, 99);
            return $"{n1}.{n2}.{n3}-{n4}";
        }

        
        public IActionResult OnGetDownload(string file)
        {
            if (string.IsNullOrEmpty(file)) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var content = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/octet-stream";
            if (file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            if (file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) contentType = "text/csv";

            
            try
            {
                
                Response.Headers["Cache-Control"] = "private, max-age=0, must-revalidate";
                var result = File(content, contentType, file);

                
                Task.Run(() => CleanupOldGeneratedFiles(TimeSpan.FromMinutes(10)));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao servir arquivo {File}", file);
                return PhysicalFile(filePath, contentType, file);
            }
        }

        private void CleanupOldGeneratedFiles(TimeSpan maxAge)
        {
            try
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var now = DateTime.Now;
                foreach (var f in Directory.EnumerateFiles(dir, "Clientes_*."))
                {
                    try
                    {
                        var info = new FileInfo(f);
                        if (now - info.CreationTime > maxAge)
                        {
                            info.Delete();
                        }
                    }
                    catch {  }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao limpar arquivos antigos em wwwroot");
            }
        }
    }
}

