-- Optional bootstrap for a fresh project. The existing study project already
-- has this responses schema (created_at is its primary key) and the games bucket.
-- Managed devices use the existing server API key without Supabase Auth.
begin;
create table if not exists public.responses (
  created_at timestamptz primary key default now(),
  participant_id text,
  user_message text,
  agent_message text,
  agent_response_time real,
  session_id uuid
);
alter table public.responses enable row level security;
insert into storage.buckets (id, name, public)
values ('games', 'games', false)
on conflict (id) do nothing;
notify pgrst, 'reload schema';
commit;
