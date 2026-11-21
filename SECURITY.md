# Política de Segurança

## Reportando vulnerabilidades

Se você encontrar uma vulnerabilidade de segurança no SKY LAND, **não abra uma issue pública**.

Entre em contato diretamente com o mantenedor pelo GitHub: [@Joaovictor18383](https://github.com/Joaovictor18383)

Inclua no relato:

- Descrição da vulnerabilidade
- Passos para reproduzir
- Impacto potencial
- Versão do aplicativo afetada

## Boas práticas do projeto

O SKY LAND foi projetado com segurança como prioridade:

- **Nenhuma credencial é solicitada** — o app não pede senhas, cookies ou tokens de contas
- **Confirmação explícita** antes de qualquer download ou instalação
- **Validação de arquivos** por tamanho e hash SHA-256 quando a fonte publica o hash
- **Sem telemetria** — nenhum dado do usuário é enviado a servidores de terceiros além das fontes oficiais de download (GitHub, MediaFire, sites dos próprios softwares)
- Dados locais ficam apenas em `%APPDATA%\SKY LAND\`
