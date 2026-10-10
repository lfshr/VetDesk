#!/bin/bash
# Run by the sql-init service on every "docker compose up". Waits for SQL Server, then runs sql-init.sql as sa.
# SQLCMDPASSWORD (sa) and APP_SQL_PASSWORD come from the server's .env; sqlcmd reads both from the environment.
set -euo pipefail

sqlcmd=/opt/mssql-tools18/bin/sqlcmd

# The backups folder is mounted here too. Docker creates it owned by root if it was missing, which SQL Server
# (running as mssql, uid 10001) can't write to.
chown 10001:0 /backup

for attempt in $(seq 1 60); do
    if "$sqlcmd" -S sql -U sa -C -b -Q "SELECT 1" > /dev/null 2>&1; then
        exec "$sqlcmd" -S sql -U sa -C -b -i /init/sql-init.sql
    fi
    echo "Waiting for SQL Server ($attempt/60)..."
    sleep 2
done

echo "SQL Server did not answer within two minutes." >&2
exit 1
