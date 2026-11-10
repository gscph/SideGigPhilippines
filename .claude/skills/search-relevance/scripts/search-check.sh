#!/usr/bin/env bash
# Runs SideGigPh's real search code (Services/Search + ReferenceData + the sample pros) in a
# throwaway console app, so relevance can be checked without the MAUI build or an emulator.
#
# Usage:
#   search-check.sh <workdir> [query ...]                 # how free text is understood
#   search-check.sh <workdir> --rank <cat> <svc|-> <town> <budget>   # ranked sample pros
# With no queries it runs the standard regression list below. <workdir> must be OUTSIDE the
# repo (use the session scratchpad); the repo's files are compiled in place, never copied.
set -euo pipefail

WORK="${1:?pass a work dir outside the repo, e.g. the session scratchpad}"
shift
# Windows-style paths (D:/...) for MSBuild under Git Bash; plain pwd elsewhere.
winpath() { (cd "$1" && { pwd -W 2>/dev/null || pwd; }); }

REPO="$(winpath "$(dirname "$0")/../../../..")"
APP="$REPO/SideGigPh.Maui"
if command -v cygpath >/dev/null 2>&1; then WORK="$(cygpath -m "$WORK")"; fi
shopt -s nocasematch
if [[ "$WORK" == "$REPO"* ]]; then
  echo "Refusing to write inside the repo: $WORK (use the session scratchpad)" >&2
  exit 2
fi
shopt -u nocasematch

PROJ="$WORK/search-check"
mkdir -p "$PROJ"
PROJ="$(winpath "$PROJ")"
cat > "$PROJ/search-check.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$APP/Models/*.cs" />
    <Compile Include="$APP/Services/ReferenceData.cs;$APP/Services/Search/SearchText.cs;$APP/Services/Search/SearchIntentParser.cs;$APP/Services/Search/SearchRanker.cs" />
    <Compile Include="$APP/Services/IProfessionalProfileRepository.cs;$APP/Services/Dev/StubProfessionalProfileRepository.cs" />
  </ItemGroup>
</Project>
EOF

cat > "$PROJ/Program.cs" <<'EOF'
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Dev;
using SideGigPh.Maui.Services.Search;

if (args.Length > 0 && args[0] == "--rank")
{
    if (args.Length < 5) { Console.Error.WriteLine("--rank <category> <service|-> <town> <budget>"); return 2; }
    var request = new SearchRequest(args[1], args[2] == "-" ? null : args[2], args[3], args[4]);
    var pros = await new StubProfessionalProfileRepository().SearchAsync(new ProfessionalSearchQuery(args[1], null));
    // No review data here: every pro gets the unreviewed prior, so this isolates job/town/budget/experience.
    var result = SearchRanker.Rank(pros, request, new Dictionary<string, ReviewSummary>());
    Console.WriteLine($"{request}  serviceFallback={result.ServiceFallback}");
    foreach (var (group, hits) in new[] { ("IN TOWN", result.InTown), ("OTHER", result.OtherTowns) })
        foreach (var h in hits)
            Console.WriteLine($"  {group,-8} {h.Pro.DisplayName,-30} score={h.Score,6:0.0} budget={h.FitsBudget,-5} job={h.OffersService}");
    return 0;
}

string[] queries = args.Length > 0 ? args :
[
    // expected trade / job in comments; "?" = should ask which kind of pro
    "aircon", "aircon cleaning", "Pa-linis ng AC", "aircond hindi lumalamig", "aircon leak",   // aircon / -, cleaning, cleaning, repair, leak
    "tubero", "barado lababo", "gripo", "tagas tubo", "poso negro",                             // plumbing / -, clog, -, leak, septic
    "walang kuryente", "electrician", "cctv", "ilaw",                                          // electrical / no-power, -, cctv, lights
    "tulo bubong", "alulod", "karpintero", "kisame", "gate", "pa weld",                        // roofing, carpentry, welder
    "tiles", "bitak pader", "pasabuy", "bayad bills", "solar", "hilot", "massage",
    "linis", "repair", "sira", "xyz",                                                           // ?, ?, ?, none
];
foreach (var q in queries)
{
    var i = SearchIntentParser.Parse(q);
    var trade = i.CategoryId ?? (i.OtherCategoryIds.Count > 0 ? "? ask" : "(none)");
    Console.WriteLine($"{q,-26} -> {trade,-15} {i.ServiceId ?? "-",-26} also: {string.Join(",", i.OtherCategoryIds)}");
}
return 0;
EOF

cd "$PROJ"
dotnet run -v q -- "$@"
