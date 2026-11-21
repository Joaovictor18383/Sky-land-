# Contribuindo com o SKY LAND

Obrigado por considerar contribuir! Este documento explica como preparar o ambiente e enviar melhorias.

## Pré-requisitos

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 **ou** apenas o SDK pela linha de comando

## Compilar

```bash
git clone https://github.com/Joaovictor18383/Sky-land-.git
cd Sky-land-
dotnet publish SkyLand.csproj -c Release -r win-x64
```

O executável é gerado em `bin\Release\win-x64\publish\`.

## Padrões do projeto

- **WPF em C#** com .NET 8, sem dependências externas de UI
- Visual definido por **dicionários de recursos**: `Themes/` (cores por tema) e `Styles/Shared.xaml` (componentes)
- Todo texto de interface em **português (Brasil)**
- Operações de download passam por `Services/` e exibem progresso na UI
- **Nunca** execute baixados sem confirmação do usuário

## Enviando uma contribuição

1. Fork do repositório
2. Crie uma branch descritiva (`feat/...` ou `fix/...`)
3. Faça commits pequenos e descritivos
4. Abra um Pull Request explicando o que mudou e por quê

## Reportando bugs

Abra uma [issue](https://github.com/Joaovictor18383/Sky-land-/issues) com:

- O que você esperava que acontecesse
- O que aconteceu de fato
- Passos para reproduzir
- Prints ou trechos do `skyland.log` (`%APPDATA%\SKY LAND\`), se houver
