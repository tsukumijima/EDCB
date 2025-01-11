-- WebAssemblyスレッド対応のため特定のファイルにCOEP/COOPヘッダをつけて返すスクリプト
dofile(mg.script_name:gsub('[^\\/]*$','')..'util.lua')

t=mg.get_var(mg.request_info.query_string,'t')
f=nil
if t=='.js' or t=='.worker.js' or t=='.wasm' then
  f=edcb.io.open(mg.script_name:gsub('[^\\/]*$','')..'ts-live'..t,'rb')
end

if not f then
  mg.write(Response(404,nil,nil,0)..'\r\n')
else
  s=f:read('*a') or ''
  f:close()
  if t=='.js' then
    -- "ts-live.js"に含まれる文字列ts-live.{worker.js,wasm}を置換
    s=s:gsub('(ts%-live)(%.worker%.js["\'])','%1.lua?t=%2'):gsub('(ts%-live)(%.wasm["\'])','%1.lua?t=%2')
  end
  ct=CreateContentBuilder(GZIP_THRESHOLD_BYTE)
  ct:Append(s)
  ct:Finish()
  mg.write(ct:Pop(Response(200,'application/'..(t=='.wasm' and 'wasm' or 'javascript'),t~='.wasm' and 'utf-8',ct.len,ct.gzip,3600)
    ..'Cross-Origin-Embedder-Policy: require-corp\r\n'
    ..'Cross-Origin-Opener-Policy: same-origin\r\n'
    ..'\r\n'))
end