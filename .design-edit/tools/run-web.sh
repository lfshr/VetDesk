#!/usr/bin/env bash
# Publishes VetDesk.Web to .design-edit/web and runs it on :5199 against the Aspire SQL container.
set -e
cd /c/repos/VetDesk
dotnet publish src/VetDesk.Web -c Release -o .design-edit/web > .design-edit/publish.log 2>&1 || { grep -E "error" .design-edit/publish.log | sort -u | head -20; exit 1; }
PW=$(dotnet user-secrets list --project src/VetDesk.AppHost 2>/dev/null | grep "sql-password" | sed 's/.* = //')
PORT=$(docker port vetdesk-sql 1433 | head -1 | sed 's/.*://')
export ConnectionStrings__VetDesk="Server=127.0.0.1,$PORT;User ID=sa;Password=$PW;TrustServerCertificate=true;Initial Catalog=VetDesk"
export VetDesk__DatabaseProvider=SqlServer VetDesk__SeedDemoData=false ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5199 AllowedHosts='*'
cd .design-edit/web && exec dotnet VetDesk.Web.dll > ../web.log 2>&1
