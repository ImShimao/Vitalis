using System.Diagnostics;
using System.Text.Json;

namespace Vitalis.Core.Performances;

/// <summary>Résultat d'un benchmark. Chaque résultat indique sa méthode, sa durée et ses limites.</summary>
public sealed record ResultatBenchmark(
    string Composant,
    string? Valeur,
    string Unite,
    double? Score,
    string Methode,
    string Limites,
    bool Mesure);

/// <summary>
/// Benchmarks simples, mesurés et répétés. Ce ne sont pas des scores officiels : ils permettent de comparer
/// deux analyses sur la même machine. Durée totale : environ 10 à 20 secondes.
/// </summary>
public static class Benchmarks
{
    private const int TailleCribleCpu = 2_000_000;
    private const int MegaOctetsRam = 256;
    private const int MegaOctetsDisque = 256;
    private const int Repetitions = 3;

    /// <summary>CPU : crible d'Ératosthène jusqu'à 2 000 000, médiane de 3 exécutions. Score = 1000 / durée (en ms).</summary>
    public static ResultatBenchmark Cpu(CancellationToken ct = default)
    {
        var temps = new List<double>();
        int premiers = 0;
        for (var i = 0; i < Repetitions; i++)
        {
            ct.ThrowIfCancellationRequested();
            var chrono = Stopwatch.StartNew();
            premiers = Crible(TailleCribleCpu);
            chrono.Stop();
            temps.Add(chrono.Elapsed.TotalMilliseconds);
        }

        var mediane = Mediane(temps);
        return new ResultatBenchmark("Processeur", $"{mediane:0} ms ({premiers} nombres premiers)", "", Math.Round(1000.0 / mediane, 2),
            "Crible d'Ératosthène jusqu'à 2 000 000, médiane de 3 exécutions, mono-thread",
            "Mesure d'un seul cœur : ne reflète ni le multi-threading ni les jeux de données réels.", true);
    }

    /// <summary>RAM : copie d'un tampon de 256 Mio, médiane de 3 exécutions, en Mio/s.</summary>
    public static ResultatBenchmark Ram(CancellationToken ct = default)
    {
        var source = new byte[MegaOctetsRam * 1024 * 1024];
        var cible = new byte[source.Length];
        Array.Fill(source, (byte)1);

        var debits = new List<double>();
        for (var i = 0; i < Repetitions; i++)
        {
            ct.ThrowIfCancellationRequested();
            var chrono = Stopwatch.StartNew();
            Buffer.BlockCopy(source, 0, cible, 0, source.Length);
            chrono.Stop();
            debits.Add(MegaOctetsRam / chrono.Elapsed.TotalSeconds);
        }

        return new ResultatBenchmark("Mémoire vive", Math.Round(Mediane(debits)).ToString("0"), "Mio/s",
            Math.Round(Mediane(debits), 0), "Copie de 256 Mio (Buffer.BlockCopy), médiane de 3 exécutions",
            "Mesure de la copie mémoire du processus : ne couvre ni la latence ni la configuration des barrettes.", true);
    }

    /// <summary>Disque : écriture puis lecture d'un fichier temporaire de 256 Mio, supprimé à la fin. Mio/s.</summary>
    public static ResultatBenchmark Disque(string dossierTemporaire, CancellationToken ct = default)
    {
        Directory.CreateDirectory(dossierTemporaire);
        var fichier = Path.Combine(dossierTemporaire, "vitalis-benchmark.tmp");
        var tampon = new byte[1024 * 1024];
        Array.Fill(tampon, (byte)7);

        try
        {
            var chronoEcriture = Stopwatch.StartNew();
            using (var flux = new FileStream(fichier, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.WriteThrough))
            {
                for (var i = 0; i < MegaOctetsDisque; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    flux.Write(tampon, 0, tampon.Length);
                }
            }
            chronoEcriture.Stop();

            var chronoLecture = Stopwatch.StartNew();
            using (var flux = new FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
            {
                while (flux.Read(tampon, 0, tampon.Length) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                }
            }
            chronoLecture.Stop();

            var ecriture = MegaOctetsDisque / chronoEcriture.Elapsed.TotalSeconds;
            var lecture = MegaOctetsDisque / chronoLecture.Elapsed.TotalSeconds;
            return new ResultatBenchmark("Disque (dossier temporaire)",
                $"écriture {ecriture:0} · lecture {lecture:0}", "Mio/s", Math.Round(lecture, 0),
                "Écriture et lecture séquentielles de 256 Mio, avec vidage (WriteThrough) à l'écriture, sur le dossier temporaire",
                "Peut être influencé par le cache Windows lors de la lecture ; ne mesure pas les accès aléatoires.", true);
        }
        finally
        {
            try
            {
                File.Delete(fichier);
            }
            catch (IOException)
            {
                // Fichier de benchmark seulement : un échec de suppression n'est pas bloquant.
            }
        }
    }

    /// <summary>GPU : aucune méthode de mesure documentée et simple n'a été retenue : pas de score inventé.</summary>
    public static ResultatBenchmark GpuIndisponible() => new(
        "Carte graphique", null, "", null, "Aucun benchmark GPU dans cette version",
        "Une mesure GPU demande une méthode à valider (API graphique, durée, charge). Non réalisée.", false);

    public static int Crible(int limite)
    {
        var estPremier = new bool[limite + 1];
        Array.Fill(estPremier, true);
        estPremier[0] = false;
        if (limite >= 1)
        {
            estPremier[1] = false;
        }

        for (var i = 2; (long)i * i <= limite; i++)
        {
            if (!estPremier[i])
            {
                continue;
            }

            for (var j = i * i; j <= limite; j += i)
            {
                estPremier[j] = false;
            }
        }

        var n = 0;
        for (var i = 0; i <= limite; i++)
        {
            if (estPremier[i])
            {
                n++;
            }
        }

        return n;
    }

    public static double Mediane(IReadOnlyList<double> valeurs)
    {
        var tries = valeurs.OrderBy(v => v).ToList();
        var milieu = tries.Count / 2;
        return tries.Count % 2 == 1 ? tries[milieu] : (tries[milieu - 1] + tries[milieu]) / 2.0;
    }
}

/// <summary>Historique des analyses de performance, pour comparer deux analyses sur la même machine.</summary>
public sealed record AnalysePerformance(DateTime Date, IReadOnlyList<ResultatBenchmark> Resultats);

public static class HistoriquePerformances
{
    /// <summary>Variation en pourcentage entre l'analyse précédente et la nouvelle, pour un composant. Null si absente.</summary>
    public static double? Variation(AnalysePerformance precedente, AnalysePerformance actuelle, string composant)
    {
        var avant = precedente.Resultats.FirstOrDefault(r => r.Composant == composant)?.Score;
        var apres = actuelle.Resultats.FirstOrDefault(r => r.Composant == composant)?.Score;
        if (avant is null || apres is null || avant == 0)
        {
            return null;
        }

        return Math.Round((apres.Value - avant.Value) / avant.Value * 100, 1);
    }

    public static IReadOnlyList<AnalysePerformance> Charger(string fichier) =>
        File.Exists(fichier)
            ? JsonSerializer.Deserialize<List<AnalysePerformance>>(File.ReadAllText(fichier)) ?? []
            : [];

    public static void Ajouter(string fichier, AnalysePerformance analyse)
    {
        var liste = Charger(fichier).ToList();
        liste.Add(analyse);
        Directory.CreateDirectory(Path.GetDirectoryName(fichier)!);
        File.WriteAllText(fichier, JsonSerializer.Serialize(liste));
    }
}
