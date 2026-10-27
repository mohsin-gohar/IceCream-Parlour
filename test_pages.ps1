$urls = @("/Home/Product", "/Home/Recipe", "/Home/Blog", "/Home/About", "/Home/Contact", "/Home/Feedback", "/Home/Bookorder", "/Home/Gallery", "/Home/Service", "/Home/Faq", "/Home/Privacy", "/")
foreach ($u in $urls) {
  $code = 0
  try { $resp = Invoke-WebRequest -Uri "http://localhost:5000$u" -UseBasicParsing -TimeoutSec 10; $code = $resp.StatusCode } catch { $code = 0 }
  Write-Output "$u => $code"
}
Write-Output "ALLDONE"
