# Cloudflare R2 Public Access Setup Guide

## Problem
R2 storage endpoints (`https://{accountId}.r2.cloudflarestorage.com/{bucket}/{key}`) are **NOT publicly accessible** in browsers. They are only for API access.

## Solution Options

### Option 1: R2 Public Development URL (Easiest for Development) ⭐

1. **Go to Cloudflare Dashboard**
   - Navigate to R2 > Your Bucket (`mesa-gh`) > Settings
   - Scroll to "Public Access" section

2. **Enable Public Development URL**
   - Click "Enable" under "Public Development URL"
   - Type "allow" when prompted to confirm
   - You'll get a URL like: `https://pub-xxxxxxxx.r2.dev`

3. **Update appsettings.json**
   ```json
   "CloudflareR2": {
     "PublicBaseUrl": "https://pub-xxxxxxxx.r2.dev"
   }
   ```

4. **Verify**
   - Upload an image
   - Check the returned URL should be: `https://pub-xxxxxxxx.r2.dev/blogs/...`
   - Open the URL in a browser - it should display the image

**Note:** The `r2.dev` subdomain is intended for development and may have rate limits. For production, use a custom domain.

### Option 2: Custom Domain (Recommended for Production) ⭐

1. **Go to Cloudflare Dashboard**
   - Navigate to R2 > Your Bucket (`mesa-gh`) > Settings
   - Scroll to "Public Access" or "Custom Domain"

2. **Add a Custom Domain**
   - Click "Connect Domain" or "Add Custom Domain"
   - Enter a subdomain (e.g., `cdn.yourdomain.com` or `images.yourdomain.com`)
   - Follow Cloudflare's DNS setup instructions

3. **Update appsettings.json**
   ```json
   "CloudflareR2": {
     "PublicBaseUrl": "https://cdn.yourdomain.com"
   }
   ```

4. **Verify**
   - Upload an image
   - Check the returned URL should be: `https://cdn.yourdomain.com/blogs/...`
   - Open the URL in a browser - it should display the image

### Option 2: Cloudflare Workers Proxy

1. **Create a Cloudflare Worker**
   ```javascript
   export default {
     async fetch(request, env) {
       const url = new URL(request.url);
       const key = url.pathname.slice(1); // Remove leading /
       
       const object = await env.MESA_GH_BUCKET.get(key);
       
       if (object === null) {
         return new Response('Object Not Found', { status: 404 });
       }
       
       const headers = new Headers();
       object.writeHttpMetadata(headers);
       headers.set('etag', object.httpEtag);
       
       return new Response(object.body, {
         headers,
       });
     },
   };
   ```

2. **Bind the R2 Bucket to Worker**
   - In Worker settings, add R2 bucket binding named `MESA_GH_BUCKET`

3. **Deploy Worker**
   - Deploy to a route like `cdn.yourdomain.com/*` or use Workers.dev domain

4. **Update appsettings.json**
   ```json
   "CloudflareR2": {
     "PublicBaseUrl": "https://your-worker.your-subdomain.workers.dev"
   }
   ```

### Option 3: R2 Public Bucket (if available)

Some R2 buckets can be configured as public buckets with direct access. Check your R2 bucket settings for "Public Access" options.

## Current Configuration

Your current `PublicBaseUrl` is set to:
```
https://ecd9df46da1554f00cccc33d27d4451d.r2.cloudflarestorage.com/mesa-gh
```

**This will NOT work** - it's not publicly accessible. You need to set up one of the options above.

## Testing

After setting up a custom domain or Workers proxy:

1. Upload an image through your API
2. Copy the returned URL
3. Open it in a browser
4. The image should display directly

If you see "Invalid argument" or connection errors, the URL is still not publicly accessible.

