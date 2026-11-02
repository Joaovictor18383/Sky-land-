namespace SkyLand
{
    /// <summary>Estado compartilhado das operações — a mesma instância alimenta todas as páginas.</summary>
    internal static class OpsStore
    {
        public static readonly OpVM Potassium = new("Baixar Potassium",
            "Executor — pacote ZIP oficial (files.potassium.pro).", "P", null,
            "pack://application:,,,/Assets/potassium.png");

        public static readonly OpVM MadiumInstall = new("Baixar / Atualizar Madium",
            "Baixa o instalador oficial e, se você quiser, já o executa.", "M", null,
            "pack://application:,,,/Assets/madium.png");

        public static readonly OpVM MadiumRepair = new("Arrumar problemas do Madium",
            "Remove a instalação antiga (com sua confirmação) e reinstala.", null, "E912");

        public static readonly OpVM MadiumDeps = new("Dependências do Madium",
            "Instala .NET 8, Visual C++ x64/x86 e WebView2 quando ausentes.", null, "E713");

        public static readonly OpVM Real = new("Baixar Real",
            "Instalador oficial — release 'real-v1' do repositório.", "R", null,
            "pack://application:,,,/Assets/real.png");

        public static readonly OpVM WebView2 = new("Instalar / verificar WebView2",
            "Componente da Microsoft exigido por executores modernos.", null, "E713");

        public static readonly OpVM AnyDesk = new("Baixar AnyDesk",
            "Acesso remoto — download direto do site oficial.", "A", null,
            "pack://application:,,,/Assets/anydesk.png");

        public static readonly OpVM Fishstrap = new("Baixar Fishstrap",
            "Busca a versão mais recente pela API oficial do GitHub.", "F", null,
            "pack://application:,,,/Assets/fishstrap.png");

        public static readonly OpVM Ram = new("Roblox Account Manager",
            "Gerenciador de contas — ZIP extraído em Downloads.", "AM", null,
            "pack://application:,,,/Assets/ram.png");

        public static readonly OpVM MultiBloxy = new("Baixar MultiBloxy",
            "Ferramenta de multilhas — v1.1.0.0 oficial.", "MB", null,
            "pack://application:,,,/Assets/multibloxy.png");

        public static readonly OpVM UpdateDownload = new("Atualização do SKY LAND",
            "Baixa a nova versão publicada no repositório oficial.", null, "E896");
    }
}
