-- Optional: only needed when using a publishable/anon key instead of the managed
-- server key. API-key holders can insert responses and read response identities and read/overwrite game saves.
begin;
grant usage on schema public to anon;
grant insert on public.responses to anon;
-- ON CONFLICT requires read access to the primary key.
grant select (created_at) on public.responses to anon;
drop policy if exists managed_client_response_read on public.responses;
create policy managed_client_response_read on public.responses for select to anon using (true);
drop policy if exists managed_client_response_insert on public.responses;
create policy managed_client_response_insert on public.responses for insert to anon with check (true);
-- Storage upsert needs insert/update/select; recovery needs select. These
-- policies cover only the existing game's filename convention in games.
drop policy if exists managed_client_game_read on storage.objects;
create policy managed_client_game_read on storage.objects for select to anon
using (bucket_id = 'games' and left(name, 5) = 'game_' and right(name, 5) = '.json');
drop policy if exists managed_client_game_insert on storage.objects;
create policy managed_client_game_insert on storage.objects for insert to anon
with check (bucket_id = 'games' and left(name, 5) = 'game_' and right(name, 5) = '.json');
drop policy if exists managed_client_game_update on storage.objects;
create policy managed_client_game_update on storage.objects for update to anon
using (bucket_id = 'games' and left(name, 5) = 'game_' and right(name, 5) = '.json')
with check (bucket_id = 'games' and left(name, 5) = 'game_' and right(name, 5) = '.json');

commit;
