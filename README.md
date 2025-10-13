GeradorDeClientes
=================

This is a small ASP.NET Core Razor Pages application that generates and emails XLSX files. This README contains guidance for preparing the repository before pushing to GitHub and how to securely store secrets.

Before you push
---------------
- Do NOT commit SMTP passwords or any other secrets. Use the recommended approaches below.
- Confirm `.gitignore` is present (it excludes `App_Data/users.json`, `appsettings.Development.json` and `wwwroot/emails_test/`).

Storing secrets locally (recommended)
GeradorDeClientes
=================

Aplicação ASP.NET Core Razor Pages que gera um arquivo Excel (.xlsx) com dados de clientes fictícios e permite enviar o arquivo por e-mail.

Importante antes de publicar
----------------------------
- Não inclua senhas nem outros segredos no repositório. Use `dotnet user-secrets` para desenvolvimento e variáveis de ambiente em produção.
- O `.gitignore` está configurado para excluir arquivos sensíveis e artefatos (ex.: `App_Data/users.json`, `appsettings.Development.json`, `wwwroot/emails_test/`).

Configuração de segredos
------------------------
1) Usando dotnet user-secrets (recomendado para desenvolvimento):

```powershell
cd C:\caminho\para\o\projeto
dotnet user-secrets init
dotnet user-secrets set "Smtp:Host" "smtp.seuprovedor.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:User" "seu-usuario@provedor.com"
dotnet user-secrets set "Smtp:Pass" "sua-senha"
dotnet user-secrets set "Smtp:ForcePickup" "false"
```

2) Usando variáveis de ambiente (CI/CD ou produção):

```powershell
$env:Smtp__Host = 'smtp.seuprovedor.com'
$env:Smtp__Port = '587'
$env:Smtp__User = 'seu-usuario@provedor.com'
$env:Smtp__Pass = 'sua-senha'
$env:Smtp__ForcePickup = 'false'
```

Publicação no GitHub
--------------------
1. Crie um repositório no GitHub (ex.: `GeradorDeClientes`).
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
--------------
1. Restaurar dependências e compilar:

```powershell
dotnet restore
dotnet build
```

2. Executar a aplicação:

```powershell
dotnet run --project .\GeradorDeClientes.csproj
```

3. Acesse `http://localhost:<porta>` conforme exibido pelo dotnet.

Observações
-----------
- O algoritmo de hash SHA256 foi usado apenas para demonstração no exemplo. Para produção, utilize um KDF adequado (PBKDF2, Argon2, bcrypt).
- Não comite credenciais no repositório. Use user-secrets ou variáveis de ambiente.
- Artefatos de build (pasta `bin/`) são gerados automaticamente; se houver necessidade de remover vestígios, execute `dotnet clean` e reconstrua.

Deploy e automação
-------------------
Para automatizar deploy com GitHub Actions, configure os secrets necessários no repositório (por exemplo, `AZURE_WEBAPP_NAME` e `AZURE_PUBLISH_PROFILE`) e adicione workflows conforme a infraestrutura desejada.

Suporte
-------
Este repositório contém a aplicação fonte e instruções para execução local. Ajustes adicionais podem ser feitos conforme necessidade do ambiente de produção.
