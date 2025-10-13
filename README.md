GeradorDeClientes
=================

This is a small ASP.NET Core Razor Pages application that generates and emails XLSX files. This README contains guidance for preparing the repository before pushing to GitHub and how to securely store secrets.

Before you push
---------------
- Do NOT commit SMTP passwords or any other secrets. Use the recommended approaches below.
- Confirm `.gitignore` is present (it excludes `App_Data/users.json`, `appsettings.Development.json` and `wwwroot/emails_test/`).

Storing secrets locally (recommended)
------------------------------------
1) Using dotnet user-secrets (per-project, recommended for development):

   # from project folder (PowerShell)
   dotnet user-secrets init
   dotnet user-secrets set "Smtp:Host" "smtp.example.com"
   dotnet user-secrets set "Smtp:Port" "587"
   dotnet user-secrets set "Smtp:User" "your-smtp-user@example.com"
   dotnet user-secrets set "Smtp:Pass" "your-smtp-password"
   dotnet user-secrets set "Smtp:ForcePickup" "false"

2) Or use environment variables (CI/CD or production):

   # PowerShell example
   $env:Smtp__Host = 'smtp.example.com'; $env:Smtp__Port = '587'; $env:Smtp__User = 'your-smtp-user@example.com'; $env:Smtp__Pass = 'your-smtp-password'; $env:Smtp__ForcePickup = 'false'

Preparing and pushing to GitHub
-------------------------------
If you want me to push the code for you, I will need either:

- A remote URL for the repository you already created (HTTPS or SSH), and your approval to run git commands locally, or
- You authenticate locally with the GitHub CLI (`gh auth login`) and I can create the remote and push.
GeradorDeClientes
=================

Aplicação em ASP.NET Core Razor Pages que gera um arquivo Excel (.xlsx) com dados fictícios de clientes e permite enviar o arquivo por e-mail.

Antes de publicar
------------------
- Não inclua senhas nem outros segredos no repositório. Utilize `dotnet user-secrets` para desenvolvimento ou variáveis de ambiente em produção.
- O arquivo `.gitignore` já está configurado para excluir `App_Data/users.json`, `appsettings.Development.json` e `wwwroot/emails_test/`.

Configuração de segredos (opções)
--------------------------------
1) dotnet user-secrets (recomendado para desenvolvimento):

   # no diretório do projeto (PowerShell)
   dotnet user-secrets init
   dotnet user-secrets set "Smtp:Host" "smtp.seuprovedor.com"
   dotnet user-secrets set "Smtp:Port" "587"
   dotnet user-secrets set "Smtp:User" "seu-usuario@provedor.com"
   dotnet user-secrets set "Smtp:Pass" "sua-senha"
   dotnet user-secrets set "Smtp:ForcePickup" "false"

2) Variáveis de ambiente (CI/CD ou produção):

   # PowerShell exemplo
   $env:Smtp__Host = 'smtp.seuprovedor.com'
   $env:Smtp__Port = '587'
   $env:Smtp__User = 'seu-usuario@provedor.com'
   $env:Smtp__Pass = 'sua-senha'
   $env:Smtp__ForcePickup = 'false'

Como publicar no GitHub
-----------------------
1. Crie um repositório no GitHub (por exemplo `GeradorDeClientes`).
2. No diretório do projeto local execute:

```powershell
git init
git add --all
git commit -m "Initial commit - GeradorDeClientes"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/GeradorDeClientes.git
git push -u origin main
```

Execução local
---------------
1. Restaurar dependências e compilar:

```powershell
dotnet restore
dotnet build
```

2. Executar a aplicação:

```powershell
dotnet run --project .\GeradorDeClientes.csproj
```

3. Acesse `http://localhost:<porta>` conforme exibido pelo dotnet e faça login com as credenciais de teste (se configuradas).

Notas importantes
-----------------
- O hash de senha usado é SHA256 apenas para demonstração. Em produção, utilize um algoritmo de derivação de chave (PBKDF2, Argon2, bcrypt).
- Não comite credenciais no repositório. Use user-secrets ou variáveis de ambiente.

Contato e deploy
----------------
Se desejar automatizar deploy ou configurar GitHub Actions, inclua os secrets apropriados no repositório e adicione workflows conforme necessário.
