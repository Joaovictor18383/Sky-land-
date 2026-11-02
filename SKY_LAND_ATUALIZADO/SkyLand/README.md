# SKY LAND

Sistema de Controle e Operações para executores Roblox.

## Requisitos

- Windows x64
- .NET 8.0 Desktop Runtime (baixado automaticamente na primeira execução)
- Microsoft Edge WebView2 (baixado automaticamente na primeira execução)
- Visual C++ Redistributable x64 e x86 (baixado automaticamente na primeira execução)

## Como Compilar

1. Abra o projeto `SkyLand.csproj` no Visual Studio 2022 ou mais recente
2. Restaure as dependências (NuGet)
3. Compile em modo Release para Windows x64:
   ```
   dotnet publish -c Release -r win-x64
   ```
4. O executável será gerado em `bin\Release\win-x64\publish\SKY LAND.exe`

## Como Executar

### Opção 1: Executável (Recomendado)
Basta executar `SKY LAND.exe`. O aplicativo verificará automaticamente as dependências e oferecerá baixar e instalar se necessário.

### Opção 2: Código Fonte
Abra o projeto no Visual Studio e execute o projeto.

## Funcionalidades

- **Potassium**: Download do executor Potassium
- **Madium**: Download, atualização e reparação do Madium
- **Real**: Download do RealSetup.exe
- **Outros**: 
  - Status dos executores (API WEAO)
  - Download do AnyDesk
  - Download do Fishstrap
  - Download do Roblox Account Manager
  - Download do MultiBloxy
- **Configurações**:
  - Pasta de downloads
  - Verificação automática de atualizações
  - Histórico de versões
  - Limpeza de arquivos temporários

## Auto-Update

O aplicativo verifica automaticamente atualizações no repositório GitHub:
https://github.com/Joaovictor18383/Sky-land-

## Identidade Visual

- Cor principal: #8B17C0 (roxo)
- Tema escuro profissional
- Ícone de alta qualidade com múltiplas resoluções

## Créditos

- Joaovictor36484
- Repositório oficial: https://github.com/Joaovictor18383/Sky-land-

## Licença

SKY LAND © 2024. Todos os direitos reservados.
