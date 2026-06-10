using LabControl.Models;
using LabControl.Models.Entidades;
using LabControl.Models.Laboratorio;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Entidades
    public DbSet<Entidade> Entidades => Set<Entidade>();
    public DbSet<EntidadePF> EntidadesPF => Set<EntidadePF>();
    public DbSet<EntidadePJ> EntidadesPJ => Set<EntidadePJ>();
    public DbSet<EntidadeFuncionario> EntidadesFuncionarios => Set<EntidadeFuncionario>();
    public DbSet<TipoRegProfissional> TiposRegProfissional => Set<TipoRegProfissional>();
    public DbSet<CargoFuncionario> CargosFuncionarios => Set<CargoFuncionario>();
    public DbSet<EntidadeFuncAssinatura> EntidadesFuncAssinaturas => Set<EntidadeFuncAssinatura>();
    public DbSet<EntidadeEndereco> EntidadesEnderecos => Set<EntidadeEndereco>();
    public DbSet<EntidadeFone> EntidadesFones => Set<EntidadeFone>();
    public DbSet<EntidadeEmail> EntidadesEmails => Set<EntidadeEmail>();
    public DbSet<EnderecoTipo> EnderecosTipos => Set<EnderecoTipo>();
    public DbSet<FoneTipo> FonesTipos => Set<FoneTipo>();

    public DbSet<EntidadeObservacao> EntidadesObservacoes => Set<EntidadeObservacao>();

    // Segurança
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AcaoUsuario> AcoesUsuarios => Set<AcaoUsuario>();
    public DbSet<Empresa> Empresas => Set<Empresa>();

    // Tabelas auxiliares do laboratório
    public DbSet<AmostraTipo> AmostrasTipos => Set<AmostraTipo>();
    public DbSet<AmostraStatus> AmostrasStatus => Set<AmostraStatus>();
    public DbSet<AnaliseTipo> AnalisesTipos => Set<AnaliseTipo>();
    public DbSet<AnaliseStatus> AnalisesStatus => Set<AnaliseStatus>();
    public DbSet<AnaliseMetodo> AnalisesMetodos => Set<AnaliseMetodo>();
    public DbSet<BoletimStatus> BoletinsStatus => Set<BoletimStatus>();
    public DbSet<Idioma> Idiomas => Set<Idioma>();
    public DbSet<Prazo> Prazos => Set<Prazo>();
    public DbSet<Moeda> Moedas => Set<Moeda>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<EmbalagemTipo> EmbalagensTopos => Set<EmbalagemTipo>();
    public DbSet<PropostaStatus> PropostasStatus => Set<PropostaStatus>();
    public DbSet<TipoResultado> TiposResultados => Set<TipoResultado>();
    public DbSet<CondicaoPagamento> CondicoesPagamentos => Set<CondicaoPagamento>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<ParametroAnalise> ParametrosAnalises => Set<ParametroAnalise>();

    // Propostas
    public DbSet<Proposta> Propostas => Set<Proposta>();
    public DbSet<PropostaAnalise> PropostasAnalises => Set<PropostaAnalise>();

    // Amostras
    public DbSet<HistAmostra> HistAmostras => Set<HistAmostra>();
    public DbSet<HistAmostraTeste> HistAmostrasTestess => Set<HistAmostraTeste>();
    public DbSet<HistAmostraSaldo> HistAmostrasaldos => Set<HistAmostraSaldo>();
    public DbSet<MovAmostra> MovAmostras => Set<MovAmostra>();
    public DbSet<LocalAmostra> LocalAmostras => Set<LocalAmostra>();

    // Resultados
    public DbSet<ResultadoAnalise> ResultadosAnalises => Set<ResultadoAnalise>();
    public DbSet<ResultadoParam> ResultadosParam => Set<ResultadoParam>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Chave composta em AcaoUsuario
        modelBuilder.Entity<AcaoUsuario>()
            .HasKey(a => new { a.IdUsuario, a.Form });

        // Relacionamento 1-1 Entidade -> Observacao
        modelBuilder.Entity<EntidadeObservacao>()
            .HasOne(o => o.Entidade)
            .WithOne(e => e.Observacao)
            .HasForeignKey<EntidadeObservacao>(o => o.IdEntidade);

        // Relacionamento 1-1 Entidade -> PF/PJ/Funcionario
        modelBuilder.Entity<EntidadePF>()
            .HasOne(e => e.Entidade)
            .WithOne(e => e.PessoaFisica)
            .HasForeignKey<EntidadePF>(e => e.Id);

        modelBuilder.Entity<EntidadePJ>()
            .HasOne(e => e.Entidade)
            .WithOne(e => e.PessoaJuridica)
            .HasForeignKey<EntidadePJ>(e => e.Id);

        modelBuilder.Entity<EntidadeFuncionario>()
            .HasOne(e => e.Entidade)
            .WithOne(e => e.Funcionario)
            .HasForeignKey<EntidadeFuncionario>(e => e.Id);

        modelBuilder.Entity<EntidadeFuncAssinatura>()
            .HasOne(a => a.Funcionario)
            .WithOne(f => f.Assinatura)
            .HasForeignKey<EntidadeFuncAssinatura>(a => a.IdEntidadeFunc);

        // Saldo: chave 1-1 com HistAmostra
        modelBuilder.Entity<HistAmostraSaldo>()
            .HasOne(s => s.HistAmostra)
            .WithOne(h => h.Saldo)
            .HasForeignKey<HistAmostraSaldo>(s => s.IdHistAmostra);

        // LocalAmostra: 1-1 com HistAmostra
        modelBuilder.Entity<LocalAmostra>()
            .HasOne(l => l.HistAmostra)
            .WithOne(h => h.LocalizacaoAtual)
            .HasForeignKey<LocalAmostra>(l => l.IdHistAmostra);

        // Decimais
        modelBuilder.Entity<Proposta>()
            .Property(p => p.VrTotal).HasColumnType("decimal(15,4)");
        modelBuilder.Entity<Proposta>()
            .Property(p => p.PorcDesconto).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Proposta>()
            .Property(p => p.VrDesconto).HasColumnType("decimal(15,4)");

        modelBuilder.Entity<PropostaAnalise>()
            .Property(p => p.VrUnitario).HasColumnType("decimal(15,4)");
        modelBuilder.Entity<PropostaAnalise>()
            .Property(p => p.VrDesconto).HasColumnType("decimal(15,4)");
        modelBuilder.Entity<PropostaAnalise>()
            .Property(p => p.VrTotal).HasColumnType("decimal(15,4)");

        modelBuilder.Entity<MovAmostra>()
            .Property(m => m.Qtde).HasColumnType("decimal(15,4)");

        modelBuilder.Entity<HistAmostraSaldo>()
            .Property(s => s.SaldoAtual).HasColumnType("decimal(15,4)");

        modelBuilder.Entity<ParametroAnalise>()
            .Property(p => p.VrUnitario).HasColumnType("decimal(15,4)");
    }
}
