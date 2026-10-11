#!/bin/zsh
# Local PostgreSQL panel for FeriaAcademica (macOS).
# Checks that PostgreSQL is installed (offers to install it with Homebrew) and gives a menu
# to start/stop it, inspect tables, copy the connection string and update dotnet user-secrets.

DB=feria_academica
PG_HOST=localhost
PG_PORT=5432
PG_USER=$(whoami)          # Homebrew creates a superuser named after the macOS user
PG_PASS=""                 # Homebrew's default setup has no password
REPO_ROOT="${0:A:h}/../.."
API_PROJECT="$REPO_ROOT/src/FeriaAcademica.Api"
FORMULA=postgresql@17

pg_bin() {
  local prefix
  prefix=$(brew --prefix $FORMULA 2>/dev/null)
  if [[ -n "$prefix" && -x "$prefix/bin/psql" ]]; then echo "$prefix/bin"; return; fi
  command -v psql >/dev/null 2>&1 && dirname "$(command -v psql)"
}

installed() { [[ -n "$(pg_bin)" ]]; }
running()   { installed && "$(pg_bin)/pg_isready" -q -h $PG_HOST -p $PG_PORT; }

conn_string() {
  local cs="Host=$PG_HOST;Port=$PG_PORT;Database=$DB;Username=$PG_USER"
  [[ -n "$PG_PASS" && "$1" == "show" ]] && cs="$cs;Password=********"
  [[ -n "$PG_PASS" && "$1" != "show" ]] && cs="$cs;Password=$PG_PASS"
  echo "$cs"
}

need_installed() { installed || { echo "PostgreSQL is not installed. Use option [8]."; return 1; } }
need_running()   { need_installed || return 1; running || { echo "PostgreSQL is stopped. Use option [1]."; return 1; } }

psql_run() { PGPASSWORD="$PG_PASS" "$(pg_bin)/psql" -h $PG_HOST -p $PG_PORT -U $PG_USER "$@"; }

install_pg() {
  if installed; then echo "PostgreSQL is already installed."; return; fi
  if ! command -v brew >/dev/null 2>&1; then
    echo "Homebrew was not found. Install it from https://brew.sh or use https://postgresapp.com"; return
  fi
  echo "Installing $FORMULA with Homebrew..."
  HOMEBREW_NO_AUTO_UPDATE=1 brew install $FORMULA && brew services start $FORMULA >/dev/null 2>&1
  for i in {1..15}; do running && break; sleep 1; done
  running && echo "PostgreSQL installed and running." || echo "Installed, but it is not running yet. Try option [1]."
}

start_pg() {
  need_installed || return
  if running; then echo "PostgreSQL is already running."; return; fi
  brew services start $FORMULA >/dev/null 2>&1
  for i in {1..15}; do running && break; sleep 1; done
  running && echo "PostgreSQL is running." || echo "It did not start. Try: brew services restart $FORMULA"
}

stop_pg() {
  need_installed || return
  brew services stop $FORMULA >/dev/null 2>&1
  echo "PostgreSQL stopped."
}

update_secrets() {
  command -v dotnet >/dev/null 2>&1 || { echo "dotnet was not found. Install the .NET SDK 10 first."; return; }
  [[ -d "$API_PROJECT" ]] || { echo "Project not found: $API_PROJECT"; return; }
  read -rs "newpass?  Password for '$PG_USER' (Enter for none): "; echo
  PG_PASS="$newpass"
  dotnet user-secrets set "ConnectionStrings:Default" "$(conn_string)" --project "$API_PROJECT" \
    && echo "dotnet user-secrets updated." && conn_string show
}

reset_db() {
  need_running || return
  read -r "ok?  This deletes ALL data in $DB. Type YES to confirm: "
  if [[ "$ok" == "YES" ]]; then
    psql_run -d postgres -c "DROP DATABASE IF EXISTS $DB WITH (FORCE);" >/dev/null \
      && echo "Database deleted. It is recreated with migrations the next time you run the API."
  else echo "Cancelled."; fi
}

if ! installed; then
  echo "PostgreSQL was not found on this computer."
  read -r "ans?Install it now with Homebrew? (y/n) "
  [[ "$ans" =~ ^[yYsS] ]] && install_pg
fi

while true; do
  clear
  if ! installed; then state="NOT INSTALLED"; elif running; then state="RUNNING"; else state="STOPPED"; fi
  echo "=============================================="
  echo "  FeriaAcademica - PostgreSQL local"
  echo "=============================================="
  echo "  Status: $state"
  echo
  echo "  Connection string:"
  echo "  $(conn_string show)"
  echo
  echo "  [1] Start PostgreSQL         brew services start $FORMULA"
  echo "  [2] Stop PostgreSQL          brew services stop $FORMULA"
  echo "  [3] List tables              psql -d $DB -c '\\dt'"
  echo "  [4] Open psql (interactive)  psql -d $DB"
  echo "  [5] Copy connection string"
  echo "  [6] Update dotnet user-secrets"
  echo "  [7] Delete and recreate the database (resets all data)"
  echo "  [8] Install PostgreSQL (Homebrew)"
  echo "  [q] Quit"
  echo
  read -r "opt?  Choose an option: "
  echo
  case "$opt" in
    1) start_pg ;;
    2) stop_pg ;;
    3) need_running && psql_run -d $DB -c '\dt' ;;
    4) need_running && psql_run -d $DB ;;
    5) need_installed && { echo -n "$(conn_string)" | pbcopy; echo "Copied to the clipboard."; } ;;
    6) update_secrets ;;
    7) reset_db ;;
    8) install_pg ;;
    q|Q) exit 0 ;;
    *) echo "Invalid option." ;;
  esac
  echo
  read -r "?  Press Enter to go back to the menu..."
done
