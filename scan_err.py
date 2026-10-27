import re
p = r"F:\.net\Ice_Cream_Parlour_Eproject\Ice_Cream_Parlour_Eproject\app_run_out.txt"
with open(p, 'rb') as f:
    data = f.read()
text = data.decode('utf-8', errors='replace')
lines = text.split('\r\n')
hits = []
for l in lines:
    low = l.lower()
    if any(k in low for k in ['error', 'exception', 'fail', 'critical']):
        hits.append(l.strip())
for h in hits[:60]:
    print(h)
print('---TOTAL LINES:', len(lines))

