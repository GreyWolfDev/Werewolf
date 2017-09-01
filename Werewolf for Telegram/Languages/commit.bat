"C:\Program Files\Git\cmd\git" fetch
"C:\Program Files\Git\cmd\git" pull origin beta --allow-unrelated-histories
"C:\Program Files\Git\cmd\git" add -A
"C:\Program Files\Git\cmd\git" commit -m %1
"C:\Program Files\Git\cmd\git" push origin master:beta