# Webserver (nginx)
``` nginx.conf
server { }
```
- Creates new server block
- Multiple server blocks can exist
``` nginx.conf
listen 80;
```
- Listens on port 80 (HTTP)
- All requests sent to port 80 are thus handled by this server block
``` nginx.conf
root /usr/share/nginx/html;
```
- Built Vue/Quasar app files are located in this root directory for the server block
- nginx looks here first when requested to server files
``` nginx.conf
index index.html;
```
- Defines default file to server if a request was not more specific
- Working together with the defined root and port to listen for, what would be server if we locally call `http://localhost/` is `/usr/share/nginx/html/index.html` as we used HTTP (port 80) and did not define a more specific file we want
``` nginx.conf
location / {
	try_files $uri $uri/ /index.html;
}
```
- Defines a location, in this case the location for all paths that start with `/` (i.e., all paths)
- Catch-all route for the frontend: All requests made to the frontend will execute the following block
- Three attempts to serve files are made: 
	- The exact file requested (`$uri`). Will fail if there is no such file at `/usr/share/nginx/html` (though `$uri` may itself also contain a further specified path/to/file)
	- An index file contained within a path `$uri` (`$uri/`). Will fail if no such directory exists at `/usr/share/nginx/html`
	- As a fallback, the `index.html` (`/index.html`)
``` nginx.conf
location /api/ {
	proxy_pass http://paperless-backend:5001/api/;
	proxy_set_header Host $host;
	proxy_set_header X-Real-IP $remote_addr;
	proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
}
```
- Defines a location, in this case the location for all paths starting with `/api/`
- This location is more specific than `location /` and thus takes precedence (it does not matter whether it is placed before or after `location /` in the code)
- `proxy_pass` sends the request forward to the specified address, in our case via HTTP to the Docker container called `paperless-backend` on port 5001, using one of the /api/ endpoints. The name paperless-backend is only known within the Docker network it and our frontend service reside in. This is the reason why we need the reverse proxy: As users, we cannot talk to `paperless-backend` directly
- `proxy_set_header` sets various headers we require. These are:
	- `Host`: The host name of the original request sender (which in our case will be `localhost`). If not set, backend would think the proxied host `paperless-backend:5001` is the actual sender
	- `X-Real-IP`: The IP address of the original request sender. If not set, backend would think the internal IP of the nginx server is the actual sender
	- `X-Forwarded-For`: A standard header for proxied requests. Displays the chain of proxy servers the request passed through. WIth `$proxy_add_x_forwarded_for`, the current client IP (who nginx received the request from) is appended to this header. This means in our case this will be the same as `X-Real-IP`. The header is nonetheless included as it is standard to do so