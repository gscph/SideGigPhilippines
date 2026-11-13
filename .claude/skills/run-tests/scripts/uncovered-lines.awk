# Lists every source line the tests never ran, as "path:line", from a Cobertura report (coverlet).
# run-tests.sh --coverage prints them under the per-file summary, with the line's code.
/<class / {
  match($0, /filename="[^"]*"/)
  file = substr($0, RSTART + 10, RLENGTH - 11)
  gsub(/\\/, "/", file)
}
/<line / && file != "" {
  match($0, /number="[0-9]+"/); n = substr($0, RSTART + 8, RLENGTH - 9)
  match($0, /hits="[0-9]+"/); h = substr($0, RSTART + 6, RLENGTH - 7) + 0
  key = file ":" n
  seen[key] = 1
  if (h > 0) hit[key] = 1
}
END {
  for (key in seen) if (!(key in hit)) print key
}
