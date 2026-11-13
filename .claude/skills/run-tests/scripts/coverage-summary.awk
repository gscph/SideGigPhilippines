# Line coverage per source file from a Cobertura report (coverlet). Each source line counts once, even when the
# compiler split it into several methods (async state machines, lambdas). Prints "pct covered/total file".
/<class / {
  match($0, /filename="[^"]*"/)
  file = substr($0, RSTART + 10, RLENGTH - 11)
  # coverlet writes some paths in full and some relative: keep them from the project folder on.
  if (match(file, /(SideGigPh\.[A-Za-z]+|MobileAppTheme)[\\\/].*/)) file = substr(file, RSTART)
  gsub(/\\/, "/", file)
}
/<line / && file != "" {
  match($0, /number="[0-9]+"/); n = substr($0, RSTART + 8, RLENGTH - 9)
  match($0, /hits="[0-9]+"/); h = substr($0, RSTART + 6, RLENGTH - 7) + 0
  key = file SUBSEP n
  if (!(key in seen)) { seen[key] = 1; total[file]++ }
  if (h > 0 && !(key in hit)) { hit[key] = 1; covered[file]++ }
}
END {
  for (f in total) {
    all += total[f]; ok += covered[f]
    printf "%5.1f%%  %4d/%-4d %s\n", 100 * covered[f] / total[f], covered[f], total[f], f
  }
  if (all > 0) printf "%5.1f%%  %4d/%-4d TOTAL\n", 100 * ok / all, ok, all
}
