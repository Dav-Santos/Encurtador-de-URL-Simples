# Encurtador de URL Simples

Projeto ASP.NET Core Minimal API para encurtar URLs, redirecionar para a URL original e exibir estatísticas básicas.

## Funcionalidades

- `POST /shorten` — encurta uma URL informada
- `GET /{code}` — redireciona para a URL original
- `GET /api/urls` — retorna todas as URLs cadastradas
- `DELETE /api/urls/{code}` — remove um código encurtado
- `GET /api/stats/{code}` — retorna estatísticas de um código

## Requisitos

- .NET 10 SDK / Runtime instalado

## Como executar

No terminal, dentro da pasta do projeto:

```powershell
cd "c:\Users\david\OneDrive\Área de Trabalho\Encurtador de URL Simples"
dotnet run
```

A API será iniciada em `https://localhost:5001` e `http://localhost:5000`.

## Testes rápidos

Exemplo de solicitação para encurtar uma URL:

```powershell
Invoke-WebRequest -Uri "http://localhost:5000/shorten" -Method POST -Body '{"Url":"https://www.google.com"}' -ContentType "application/json"
```

## Observações

- O armazenamento é em memória; ao reiniciar o aplicativo, os dados são perdidos.
- Para publicar no GitHub, crie um repositório remoto e adicione-o como `origin`.
