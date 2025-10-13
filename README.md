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

Commands I will run (with your approval):

```powershell
git init
git add --all
git commit -m "Initial commit"
git branch -M main
git remote add origin <REMOTE_URL>
git push -u origin main
```

If you already created a remote repository, replace `<REMOTE_URL>` with your repository URL.

Security note
-------------
Never share passwords directly in chat. Use `dotnet user-secrets` or environment variables. If secrets were accidentally committed, we can remove them from history, but it's easier to avoid committing them in the first place.
# GeradorDeClientes

Aplicação simples em ASP.NET Core para gerar dados fictícios em Excel e enviar por email.

## Funcionalidades
- Login com usuário demo (admin@teste.com / 123)
- Gerar arquivo Excel (.xlsx) com clientes fictícios
- Enviar o arquivo por email (configurar SMTP)

## Configuração e execução local

1. Restaurar dependências e build:

```powershell
dotnet restore
dotnet build
```

2. Configurar SMTP (localmente, usando user-secrets recomendado):

```powershell
cd C:\Users\Michel\Desktop\GeradorDeClientes
dotnet user-secrets init
# Substitua pelos valores reais
dotnet user-secrets set "Smtp:Host" "smtp.seuprovedor.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:User" "seu-usuario@provedor.com"
dotnet user-secrets set "Smtp:Pass" "sua-senha"
dotnet user-secrets set "Smtp:EnableSsl" "true"
dotnet user-secrets set "Smtp:ForcePickup" "false"

# Configure também o endereço do repositório e da aplicação publicada (serão incluídos no corpo do e-mail):
dotnet user-secrets set "RepoUrl" "https://github.com/SEU_USUARIO/GeradorDeClientes"
dotnet user-secrets set "DeployUrl" "https://seu-app-publicado.example.com"
```

3. Rodar a aplicação:

```powershell
dotnet run --project .\GeradorDeClientes.csproj
```

4. Acesse http://localhost:5000/Login (ou a porta indicada pelo dotnet) e faça login com:

- Email: admin@teste.com
- Senha: 123

## Envio de email

- O sistema enviará o e-mail para `sergio.junior@atak.com.br` com assunto "[GeradorDeClientes] - Dados Gerados".
- No corpo do e-mail serão incluídos os links `RepoUrl` e `DeployUrl` configurados via user-secrets.

## Publicar no GitHub

1. Crie um repositório público no GitHub chamado `GeradorDeClientes`.
2. No seu diretório do projeto local:

```powershell
git init
git add .
git commit -m "Initial commit - GeradorDeClientes"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/GeradorDeClientes.git
git push -u origin main
```

3. (Opcional) Habilitar GitHub Actions: já incluí um workflow em `.github/workflows/deploy-to-azure.yml` para deploy quando você configurar secrets.

## Publicar no Azure (resumo)

1. Crie um recurso App Service (Windows ou Linux) no Azure (plano gratuito disponível).
2. No portal do Azure, vá em "Deployment Center" ou baixe o "Publish Profile" (.PublishSettings / XML).
3. No GitHub, adicione dois secrets no repositório: `AZURE_WEBAPP_NAME` e `AZURE_PUBLISH_PROFILE` (o conteúdo do publish profile XML).
4. Ao dar push na branch `main`, o workflow `.github/workflows/deploy-to-azure.yml` fará build e deploy automaticamente.

## Template de email para enviar ao Sergio

Assunto: GeradorDeClientes - Repositório e Deploy

Corpo:

Olá Sergio,

Seguem os links do projeto GeradorDeClientes:

Repositório: https://github.com/SEU_USUARIO/GeradorDeClientes
Aplicação publicada: https://SEU_APP.azurewebsites.net

Atenciosamente,
Michel

## Observações de Segurança

- Para demo usamos SHA256 para o hash da senha. Em produção use um KDF robusto (PBKDF2, Argon2, bcrypt).
- Não comite credenciais no repositório. Use user-secrets ou variáveis de ambiente.

---

Se quiser, eu posso:
- Ajudar a criar o repositório GitHub (te passo os comandos e você executa) e configurar os secrets.
- Ajudar a publicar no Azure (passo a passo guiado; preciso que você baixe o Publish Profile e cole o conteúdo no secret `AZURE_PUBLISH_PROFILE`).
git remote add origin https://github.com/SEU_USUARIO/GeradorDeClientes.git
git push -u origin main
```

3. (Opcional) Habilitar GitHub Actions: já incluí um workflow em `.github/workflows/deploy-to-azure.yml` para deploy quando você configurar secrets.

## Publicar no Azure (resumo)

1. Crie um recurso App Service (Windows ou Linux) no Azure (plano gratuito disponível).
2. No portal do Azure, vá em "Deployment Center" ou baixe o "Publish Profile" (.PublishSettings / XML).
3. No GitHub, adicione dois secrets no repositório: `AZURE_WEBAPP_NAME` e `AZURE_PUBLISH_PROFILE` (o conteúdo do publish profile XML).
4. Ao dar push na branch `main`, o workflow `.github/workflows/deploy-to-azure.yml` fará build e deploy automaticamente.

## Template de email para enviar ao Sergio

Envie um e-mail para `sergio.junior@atak.com.br` com o link do repositório e o link da aplicação publicada usando o template abaixo.

Assunto: GeradorDeClientes - Repositório e Deploy

Corpo:

Olá Sergio,

Seguem os links do projeto GeradorDeClientes:

Repositório: https://github.com/SEU_USUARIO/GeradorDeClientes
Aplicação publicada: https://SEU_APP.azurewebsites.net

Atenciosamente,
Michel

## Observações de Segurança

- Para demo usamos SHA256 para o hash da senha. Em produção use um KDF robusto (PBKDF2, Argon2, bcrypt).
- Não comite credenciais no repositório. Use user-secrets ou variáveis de ambiente.

---

Se quiser, eu posso:
- Ajudar a criar o repositório GitHub (te passo os comandos e você executa) e configurar os secrets.
- Ajudar a publicar no Azure (passo a passo guiado; preciso que você baixe o Publish Profile e cole o conteúdo no secret `AZURE_PUBLISH_PROFILE`).
