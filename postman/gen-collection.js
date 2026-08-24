// Generates tss-play-gateway.postman_collection.json next to this script.
// The request and query keys are read from the SDK source in ../sdk/csharp; only the
// example payloads live in the tables below, and generation fails when the two get out
// of sync. See README.md: edit this script and rerun, never the generated JSON.
const fs = require('fs');
const path = require('path');

const RK = 'tss.postman.{{location_uuid}}.{{setup_uuid}}.{{screen_id}}';

function publishItem(name, exchange, routingKey, payload, props) {
  const body = {
    properties: Object.assign({ content_type: 'application/x-protobuf' }, props),
    routing_key: routingKey,
    payload: payload,
    payload_encoding: 'string',
  };
  return {
    name,
    request: {
      method: 'POST',
      header: [],
      body: {
        mode: 'raw',
        raw: JSON.stringify(body, null, 4),
        options: { raw: { language: 'json' } },
      },
      url: {
        raw: `{{base_url}}/api/exchanges/%2f/${exchange}/publish`,
        host: ['{{base_url}}'],
        path: ['api', 'exchanges', '%2f', exchange, 'publish'],
      },
    },
    response: [],
  };
}

function command(topicAction, payload) {
  return publishItem(topicAction, 'tss-requests-publish', `${RK}.${topicAction}`, payload, { expiration: '5000' });
}

function query(topicAction, payload) {
  return publishItem(topicAction, 'tss-api-publish', `${RK}.${topicAction}`, payload, {
    expiration: '10000',
    correlation_id: '{{$guid}}',
    headers: { 'tss-reply-to': '{{reply_key}}' },
  });
}

const assetIds = {
  integrationId: '"integrationId": "{{asset_integration_id}}"',
  uuid: '"uuid": "{{asset_uuid}}"',
};

// Example payloads for asset commands: the value is the payload after the id field
// ('' when the id is the whole payload). Both id variants are generated per command.
const assetPayloadExtras = {
  'asset.show': '',
  'asset.hide': '',
  'asset.play': '',
  'asset.pause': '',
  'asset.mute': '',
  'asset.unmute': '',
  'asset.enlarge': '',
  'asset.shrink': '',
  'asset.volume': ', "normalizedVolume": 0.5',
  'asset.seek': ', "normalizedProgress": 0.5',
  'asset.page': ', "page": 1',
  'asset.move': ', "left": 0, "bottom": 0',
  'asset.crop': ', "left": 0, "top": 0, "right": 0, "bottom": 0',
};

// Example payloads for the other commands.
const commandPayloads = {
  'story.start': '{"storyUuid": "{{story_uuid}}", "suppressPlaybackTracking": false}',
  'story.stop': '{}',
  'story.volume': '{"volume": 0.5}',
  'scene.change': '{"uuid": "<scene-uuid>"}',
  'scene.next': '{}',
  'scene.previous': '{}',
  'scene.skip-transition': '{}',
  'trigger.fire': '{"triggerUuid": "<trigger-uuid>", "state": true}',
};

const queryPayloads = {
  'instance.info': '{}',
  'story.status': '{}',
  'story.available-stories': '{"sortDir": 0, "sortBy": 0, "search": "", "offset": 0, "limit": 20}',
  'story.available-story': '{"uuid": "{{story_uuid}}"}',
  'story.available-scenes': '{}',
  'story.corrupted-assets': '{}',
  'story.data': '{"uuid": "{{story_uuid}}", "version": 1}',
  'scene.status': '{}',
};

// The request and query surface is whatever the SDK exposes: every RequestAsync/CallAsync
// call site carries its topic and action. ByUuid variants share a key, hence the Set.
function sdkKeys(file) {
  const source = fs.readFileSync(path.join(__dirname, '..', 'sdk', 'csharp', file), 'utf8');
  const keys = new Set();
  for (const match of source.matchAll(/(?:RequestAsync|CallAsync)\("([a-z-]+)", "([a-z-]+)"/g)) {
    keys.add(`${match[1]}.${match[2]}`);
  }
  return keys;
}

function assertInSync(sdk, table, what) {
  const missing = [...sdk].filter(key => !table.has(key));
  const stale = [...table].filter(key => !sdk.has(key));
  if (missing.length || stale.length) {
    throw new Error(`${what} payload table out of sync with the SDK.`
      + (missing.length ? ` Add entries for: ${missing.join(', ')}.` : '')
      + (stale.length ? ` Remove stale entries: ${stale.join(', ')}.` : ''));
  }
}

assertInSync(sdkKeys('GatewayRequests.cs'), new Set([...Object.keys(commandPayloads), ...Object.keys(assetPayloadExtras)]), 'Command');
assertInSync(sdkKeys('GatewayQueries.cs'), new Set(Object.keys(queryPayloads)), 'Query');

function topicCommands(topic) {
  return Object.entries(commandPayloads).filter(([key]) => key.startsWith(`${topic}.`)).map(([key, payload]) => command(key, payload));
}

const storyCommands = topicCommands('story');
const sceneCommands = topicCommands('scene');
const triggerCommands = topicCommands('trigger');

function assetCommands(id) {
  return Object.entries(assetPayloadExtras).map(([key, extra]) => command(key, `{${id}${extra}}`));
}

const queries = Object.entries(queryPayloads).map(([key, payload]) => query(key, payload));

// Runs before every request in the Queries folder: idempotently ensures the
// durable reply queue exists and is bound to tss-response-listen.
const PREREQ = String.raw`var base = pm.variables.get('base_url');
var q = pm.variables.get('reply_queue');
var auth = { type: 'basic', basic: [
    { key: 'username', value: pm.variables.get('user') },
    { key: 'password', value: pm.variables.get('pass') }
] };
var jsonHeader = [{ key: 'Content-Type', value: 'application/json' }];
pm.sendRequest({
    url: base + '/api/queues/%2f/' + q,
    method: 'PUT',
    auth: auth,
    header: jsonHeader,
    body: { mode: 'raw', raw: JSON.stringify({ durable: true, auto_delete: false, arguments: {} }) }
}, function () {
    pm.sendRequest({
        url: base + '/api/bindings/%2f/e/tss-response-listen/q/' + q,
        method: 'POST',
        auth: auth,
        header: jsonHeader,
        body: { mode: 'raw', raw: JSON.stringify({ routing_key: pm.variables.get('reply_key'), arguments: {} }) }
    }, function () {});
});`;

// Shared by the Queries folder test script: decodes protobuf Response payloads to
// readable JSON. Schema mirrors requests.proto + models.proto.
const DECODER_CORE = String.raw`var ENUMS = {
    State: ['NONE', 'STARTING', 'RUNNING', 'CLOSING', 'TRANSITIONING', 'ERROR', 'PREPARING_TRANSITION'],
    AvailableStoryStatus: ['UNKNOWN', 'INITIALIZING', 'READY', 'IN_USE', 'INITIALIZATION_ERROR', 'DRAFT', 'UPDATING'],
    CorruptedAssetType: ['UNDEFINED', 'VIDEO', 'IMAGE', 'TEXT', 'SOUND', 'URL', 'NDI', 'PDF'],
    UrlTouchType: ['START', 'END', 'MOVE'],
    NavigationType: ['BACK', 'FORWARD', 'REFRESH', 'HOME'],
    TextHorizontalAlignment: ['LEFT', 'CENTER', 'RIGHT'],
    TextVerticalAlignment: ['TOP', 'MIDDLE', 'BOTTOM'],
    ActionType: ['SCENE_GOTO', 'ASSET_TOGGLE', 'ASSET_SHOW', 'ASSET_HIDE', 'SCENE_NEXT', 'SCENE_PREV'],
    EffectType: ['DEFAULT', 'FADE', 'DIRECT', 'PUSH_LEFT', 'PUSH_RIGHT', 'PUSH_UP', 'PUSH_DOWN', 'COVER_LEFT', 'COVER_RIGHT', 'COVER_UP', 'COVER_DOWN', 'UNCOVER_LEFT', 'UNCOVER_RIGHT', 'UNCOVER_UP', 'UNCOVER_DOWN', 'WIPE_LEFT', 'WIPE_RIGHT', 'WIPE_UP', 'WIPE_DOWN', 'CURTAIN_HORIZONTAL', 'CURTAIN_VERTICAL', 'FALL_OVER', 'CUBE', 'BATMAN'],
    EaseType: ['LINEAR', 'SMOOTH', 'BUTTERSMOOTH', 'EXTRA_BUTTERSMOOTH', 'QUAD_IN', 'QUAD_OUT', 'QUAD_IN_OUT', 'CUBIC_IN', 'CUBIC_OUT', 'CUBIC_IN_OUT', 'QUART_IN', 'QUART_OUT', 'QUART_IN_OUT', 'QUAD_CIRC_IN', 'QUAD_CIRC_OUT', 'QUAD_CIRC_IN_OUT', 'CUBIC_CIRC_IN', 'CUBIC_CIRC_OUT', 'CUBIC_CIRC_IN_OUT', 'QUART_CIRC_IN', 'QUART_CIRC_OUT', 'QUART_CIRC_IN_OUT', 'BOUNCE_IN', 'BOUNCE_OUT', 'BOUNCE_IN_OUT', 'ELASTIC_IN', 'ELASTIC_OUT', 'ELASTIC_IN_OUT', 'ASYNC_SMOOTH']
};
var MSG = {
    Response: { 1: 'error:Error', 2: 'ok:Ok', 3: 'storyStatus:StoryStatusResponse', 4: 'availableStories:AvailableStoriesResponse', 5: 'availableScenes:AvailableScenesResponse', 6: 'currentState:CurrentStateResponse', 7: 'instanceInfo:InstanceInfo', 8: 'corruptedAssets:CorruptedAssetsResponse', 9: 'storyData:StoryDataResponse', 10: 'availableStory:AvailableStory' },
    Ok: {},
    Error: { 1: 'message:s', 2: 'code:i' },
    StoryStatusResponse: { 1: 'status:E.State', 2: 'storyTitle:s', 3: 'storyVersion:i', 4: 'transitionDurationMs:i', 5: 'storyUuid:s' },
    AvailableStoriesResponse: { 1: 'stories:*AvailableStory', 2: 'count:i' },
    AvailableStory: { 1: 'uuid:s', 2: 'name:s', 3: 'author:s', 4: 'authoredAt:Timestamp', 5: 'compiledAt:Timestamp', 6: 'description:s', 7: 'status:E.AvailableStoryStatus', 8: 'lastPlayedAt:Timestamp', 9: 'availableVersions:*i', 10: 'thumbnails:Thumbnails' },
    AvailableScenesResponse: { 1: 'scenes:*AvailableScene', 2: 'screensaverUuid:s' },
    AvailableScene: { 1: 'uuid:s', 2: 'title:s', 10: 'thumbnails:Thumbnails' },
    CurrentStateResponse: { 1: 'scene:Scene', 2: 'assets:*Asset', 3: 'storyTitle:s', 4: 'volume:f' },
    InstanceInfo: { 1: 'screenName:s', 2: 'setupName:s', 3: 'width:i', 4: 'height:i', 5: 'setupUuid:s', 6: 'screenId:i' },
    CorruptedAssetsResponse: { 1: 'scenes:*CorruptedScene' },
    CorruptedScene: { 1: 'uuid:s', 2: 'integrationId:s', 3: 'name:s', 4: 'assets:*CorruptedAsset', 5: 'index:i' },
    CorruptedAsset: { 1: 'uuid:s', 2: 'integrationId:s', 3: 'name:s', 4: 'code:i', 5: 'type:E.CorruptedAssetType' },
    StoryDataResponse: { 1: 'story:Story' },
    Story: { 1: 'uuid:s', 2: 'title:s', 3: 'version:i', 4: 'activeSceneUuid:s', 5: 'volume:f', 6: 'screensaver:Screensaver', 7: 'scenes:*Scene', 8: 'assets:*Asset', 9: 'triggers:*Trigger', 10: 'integrationId:s', 11: 'groups:*Group' },
    Group: { 1: 'uuid:s', 2: 'name:s', 3: 'baseLayer:i', 4: 'inScenes:*s', 5: 'assets:*s' },
    Screensaver: { 1: 'uuid:s', 2: 'delay:i' },
    Scene: { 1: 'uuid:s', 2: 'title:s', 3: 'autoSwitch:b', 4: 'assets:*s', 5: 'effectTransition:EffectTransition', 6: 'customTransition:CustomTransition', 7: 'integrationId:s', 8: 'thumbnails:Thumbnails' },
    CustomTransition: { 1: 'duration:i', 2: 'assets:*s' },
    EffectTransition: { 1: 'effectType:E.EffectType', 2: 'duration:i', 3: 'easeType:E.EaseType', 4: 'feather:f', 5: 'playPerSegment:b' },
    Thumbnails: { 1: 'default:s', 2: 'large:s' },
    Asset: { 1: 'uuid:s', 2: 'title:s', 3: 'layer:i', 4: 'opacity:f', 6: 'draggable:b', 7: 'roundedCorners:b', 8: 'visible:b', 9: 'rectangle:Rectangle', 10: 'margins:Margins', 11: 'urlData:UrlData', 12: 'videoData:VideoData', 13: 'textData:TextData', 14: 'soundData:SoundData', 15: 'pdfData:PdfData', 16: 'ndiData:NdiData', 17: 'imageData:ImageData', 18: 'integrationId:s', 19: 'hasOnClickActions:b', 20: 'thumbnails:Thumbnails' },
    Rectangle: { 1: 'x:f', 2: 'y:f', 3: 'width:f', 4: 'height:f' },
    Margins: { 1: 'left:i', 2: 'right:i', 3: 'top:i', 4: 'bottom:i' },
    UrlData: { 1: 'urlNavigation:b', 2: 'url:s', 3: 'volume:f', 4: 'back:b', 5: 'forward:b', 6: 'home:b', 7: 'inputFocus:b', 8: 'streamActive:b', 9: 'requests:*UrlRequest', 10: 'isMuted:b' },
    VideoData: { 1: 'play:b', 3: 'loop:b', 4: 'split:b', 5: 'volume:f', 6: 'progress:d', 7: 'duration:i', 8: 'codec:s', 9: 'files:*FileData', 10: 'isLoaded:b', 11: 'isMuted:b' },
    TextData: { 1: 'horizontalAlignment:E.TextHorizontalAlignment', 2: 'verticalAlignment:E.TextVerticalAlignment', 3: 'color:s', 4: 'font:s', 5: 'size:i', 6: 'text:s', 7: 'bold:b', 8: 'italic:b', 9: 'underline:b', 10: 'fontFile:s', 11: 'lineHeight:f' },
    SoundData: { 1: 'play:b', 2: 'loop:b', 3: 'file:FileData', 4: 'duration:i', 5: 'volume:f', 6: 'progress:d', 7: 'isMuted:b' },
    PdfData: { 1: 'page:i', 2: 'pageCount:i', 3: 'file:FileData' },
    NdiData: { 1: 'sourceName:s', 2: 'visibleOnConnection:b', 3: 'volume:f', 4: 'connected:b', 5: 'isMuted:b' },
    ImageData: { 1: 'files:*FileData' },
    FileData: { 1: 'uuid:s', 2: 'version:i', 3: 'name:s', 4: 'path:s' },
    UrlRequest: { 1: 'navigation:E.NavigationType', 2: 'key:s', 3: 'touch:UrlTouch', 4: 'internal:b' },
    UrlTouch: { 1: 'type:E.UrlTouchType', 2: 'x:f', 3: 'y:f', 4: 'touchId:i' },
    Trigger: { 1: 'uuid:s', 2: 'actions:*Action' },
    Action: { 1: 'uuid:s', 2: 'screenId:i', 3: 'type:E.ActionType' },
    Timestamp: { 1: 'seconds:i', 2: 'nanos:i' }
};
function b64bytes(s) {
    var T = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
    s = s.replace(/[^A-Za-z0-9+\/]/g, '');
    var out = [], i = 0;
    while (i < s.length) {
        var a = T.indexOf(s[i++]), b = T.indexOf(s[i++]), c = T.indexOf(s[i++]), d = T.indexOf(s[i++]);
        out.push((a << 2) | (b >> 4));
        if (c >= 0) out.push(((b & 15) << 4) | (c >> 2));
        if (d >= 0) out.push(((c & 3) << 6) | d);
    }
    return new Uint8Array(out);
}
function utf8(bytes) {
    if (typeof TextDecoder === 'function') return new TextDecoder().decode(bytes);
    var s = '', i = 0, c;
    while (i < bytes.length) {
        c = bytes[i++];
        if (c < 128) s += String.fromCharCode(c);
        else if (c < 224) s += String.fromCharCode(((c & 31) << 6) | (bytes[i++] & 63));
        else if (c < 240) s += String.fromCharCode(((c & 15) << 12) | ((bytes[i++] & 63) << 6) | (bytes[i++] & 63));
        else {
            var cp = (((c & 7) << 18) | ((bytes[i++] & 63) << 12) | ((bytes[i++] & 63) << 6) | (bytes[i++] & 63)) - 65536;
            s += String.fromCharCode(55296 + (cp >> 10), 56320 + (cp & 1023));
        }
    }
    return s;
}
function varint(buf, pos) {
    // BigInt path handles negative int32/int64 (10-byte two's complement varints) exactly.
    // No BigInt literals: they would be a parse error in an old sandbox even when unreached.
    if (typeof BigInt === 'function') {
        var r = BigInt(0), sh = BigInt(0), b;
        do { b = buf[pos++]; r += BigInt(b & 127) << sh; sh += BigInt(7); } while (b & 128);
        if (r >= BigInt('9223372036854775808')) r -= BigInt('18446744073709551616');
        return [Number(r), pos];
    }
    var r2 = 0, sh2 = 0, b2;
    do { b2 = buf[pos++]; r2 += (b2 & 127) * Math.pow(2, sh2); sh2 += 7; } while (b2 & 128);
    return [r2, pos];
}
function isPacked(type) {
    return type === 'i' || type === 'b' || type.slice(0, 2) === 'E.';
}
function conv(v, type) {
    if (type === 'b') return !!v;
    if (type.slice(0, 2) === 'E.') {
        var e = ENUMS[type.slice(2)];
        return (e && e[v] !== undefined) ? e[v] : v;
    }
    return v;
}
function round6(v) { return Math.round(v * 1e6) / 1e6; }
function decode(buf, msgName) {
    var fields = MSG[msgName] || {};
    var out = {}, pos = 0, r, val;
    while (pos < buf.length) {
        r = varint(buf, pos); pos = r[1];
        var fn = Math.floor(r[0] / 8), wt = r[0] & 7;
        var spec = fields[fn] || ('field_' + fn + ':?');
        var ci = spec.indexOf(':');
        var name = spec.slice(0, ci), type = spec.slice(ci + 1);
        var rep = type[0] === '*';
        if (rep) type = type.slice(1);
        if (wt === 0) { r = varint(buf, pos); val = conv(r[0], type); pos = r[1]; }
        else if (wt === 5) { val = round6(new DataView(buf.buffer, buf.byteOffset + pos, 4).getFloat32(0, true)); pos += 4; }
        else if (wt === 1) { val = round6(new DataView(buf.buffer, buf.byteOffset + pos, 8).getFloat64(0, true)); pos += 8; }
        else if (wt === 2) {
            r = varint(buf, pos); pos = r[1];
            var sub = buf.subarray(pos, pos + r[0]); pos += r[0];
            if (type === 'Timestamp') {
                var t = decode(sub, 'Timestamp');
                // Negative seconds (dates before 1970, e.g. DateTime.MinValue for "never") show as null.
                val = (t.seconds > 0 && t.seconds < 253402300800) ? new Date(t.seconds * 1000 + Math.round((t.nanos || 0) / 1e6)).toISOString() : null;
            }
            else if (MSG[type]) val = decode(sub, type);
            else if (isPacked(type)) {
                val = [];
                var p = 0;
                while (p < sub.length) { r = varint(sub, p); val.push(conv(r[0], type)); p = r[1]; }
            }
            else val = utf8(sub);
        }
        else break;
        if (rep) {
            if (!out[name]) out[name] = [];
            if (Array.isArray(val) && isPacked(type)) out[name] = out[name].concat(val);
            else out[name].push(val);
        } else out[name] = val;
    }
    return out;
}
function decodeAll(msgs) {
    return msgs.map(function (m) {
        var out = {
            routing_key: m.routing_key,
            correlation_id: m.properties && m.properties.correlation_id
        };
        // Never throw: the message is already consumed from the queue, so a decoder
        // bug must surface as an error entry instead of losing the payload.
        try {
            out.response = m.payload_encoding === 'base64' ? decode(b64bytes(m.payload), 'Response') : m.payload;
        } catch (e) {
            out.decode_error = String(e);
            out.payload_base64 = m.payload;
        }
        return out;
    });
}
function show(decoded) {
    console.log(JSON.stringify(decoded, null, 2));
    pm.visualizer.set('<pre style="font-size:12px;background:#1e1e1e;color:#d4d4d4;padding:10px;border-radius:4px;">{{json}}</pre>', { json: JSON.stringify(decoded, null, 2) });
}`;

// Folder-level test script: after publishing a query it polls the reply queue and
// shows the decoded reply on the query request itself; for "Get replies" (array
// response) it just decodes what was fetched.
const FOLDER_TEST = DECODER_CORE + '\n' + String.raw`var body;
try { body = pm.response.json(); } catch (e) { body = null; }
if (Array.isArray(body)) {
    show(decodeAll(body));
} else if (body && body.routed === true) {
    var cid = '';
    try { cid = JSON.parse(pm.request.body.raw).properties.correlation_id || ''; } catch (e) {}
    var auth = { type: 'basic', basic: [
        { key: 'username', value: pm.variables.get('user') },
        { key: 'password', value: pm.variables.get('pass') }
    ] };
    var url = pm.variables.get('base_url') + '/api/queues/%2f/' + pm.variables.get('reply_queue') + '/get';
    var collected = [];
    var attempts = 20;
    var poll = function () {
        pm.sendRequest({
            url: url, method: 'POST', auth: auth,
            header: [{ key: 'Content-Type', value: 'application/json' }],
            body: { mode: 'raw', raw: JSON.stringify({ count: 10, ackmode: 'ack_requeue_false', encoding: 'base64' }) }
        }, function (err, res) {
            var msgs = [];
            if (!err) { try { msgs = res.json(); } catch (e) {} }
            if (msgs.length) collected = collected.concat(decodeAll(msgs));
            var cidKnown = cid && cid.indexOf('{{') === -1;
            var done = cidKnown
                ? collected.some(function (d) { return d.correlation_id === cid; })
                : collected.length > 0;
            if (done || --attempts <= 0) {
                pm.test('reply received', function () { pm.expect(collected.length).to.be.above(0); });
                if (!collected.length) console.log('No reply within 10s; try "Get replies" manually and check the tss-play logs.');
                show(collected);
            } else {
                setTimeout(poll, 500);
            }
        });
    };
    poll();
}`;

const getReplies = {
  name: 'Get replies',
  request: {
    method: 'POST',
    header: [],
    body: {
      mode: 'raw',
      raw: JSON.stringify({ count: 10, ackmode: 'ack_requeue_false', encoding: 'base64' }, null, 4),
      options: { raw: { language: 'json' } },
    },
    url: {
      raw: '{{base_url}}/api/queues/%2f/{{reply_queue}}/get',
      host: ['{{base_url}}'],
      path: ['api', 'queues', '%2f', '{{reply_queue}}', 'get'],
    },
  },
  response: [],
};

const scopeNote = [
  'Routing key scopes (consumer binds all three):',
  '- screen:   tss.postman.<loc>.<setup>.<screen>.<topic>.<action>',
  '- setup:    tss.postman.<loc>.<setup>.<topic>.<action>  (drop the screen segment)',
  '- location: tss.postman.<loc>.<topic>.<action>          (drop setup + screen)',
  'The <source> segment (postman) is free-form. Handler dispatch uses only the last two segments.',
].join('\n');

const collection = {
  info: {
    name: 'tss-play-gateway',
    description: [
      'Publishes gateway messages to tss-play via the RabbitMQ Management API.',
      'Payloads are JSON strings; tss-play accepts JSON as well as protobuf (first-byte sniff).',
      'Enums must be sent as integers (or C# PascalCase names), not proto SCREAMING_CASE.',
      '',
      scopeNote,
      '',
      'Queries are one-shot: the folder pre-request script auto-creates and binds the durable',
      'reply queue, and the folder test script polls the queue and decodes the protobuf reply.',
      'Open the Visualize tab (or Console) on the query response to see it as JSON.',
      '"Get replies" is a manual fallback to drain the queue.',
    ].join('\n'),
    schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
  },
  item: [
    { name: 'Commands - story', item: storyCommands },
    { name: 'Commands - scene', item: sceneCommands },
    { name: 'Commands - asset integrationId', description: 'AssetRequest is a oneof: send either "integrationId" or "uuid", never both.', item: assetCommands(assetIds.integrationId) },
    { name: 'Commands - asset uuid', description: 'AssetRequest is a oneof: send either "integrationId" or "uuid", never both.', item: assetCommands(assetIds.uuid) },
    { name: 'Commands - trigger', item: triggerCommands },
    {
      name: 'Queries',
      description: 'Pre-request script ensures the reply queue exists and is bound (idempotent). available-stories: "limit" defaults to 0 server-side, so always send it.',
      event: [
        {
          listen: 'prerequest',
          script: { type: 'text/javascript', exec: PREREQ.split('\n') },
        },
        {
          listen: 'test',
          script: { type: 'text/javascript', exec: FOLDER_TEST.split('\n') },
        },
      ],
      item: [...queries, getReplies],
    },
  ],
  auth: {
    type: 'basic',
    basic: [
      { key: 'password', value: '{{pass}}', type: 'string' },
      { key: 'username', value: '{{user}}', type: 'string' },
    ],
  },
  event: [
    {
      listen: 'test',
      script: {
        type: 'text/javascript',
        exec: [
          "try {",
          "    const j = pm.response.json();",
          "    if (j && typeof j.routed === 'boolean') {",
          "        pm.test('message routed to a queue', () => pm.expect(j.routed).to.be.true);",
          "    }",
          "} catch (e) { /* non-JSON response (queue get etc.) */ }",
        ],
      },
    },
  ],
  variable: [
    { key: 'base_url', value: 'http://localhost:15672' },
    { key: 'user', value: 'user' },
    { key: 'pass', value: 'pass' },
    { key: 'location_uuid', value: '' },
    { key: 'setup_uuid', value: '' },
    { key: 'screen_id', value: '' },
    { key: 'story_uuid', value: '' },
    { key: 'asset_integration_id', value: '' },
    { key: 'asset_uuid', value: '' },
    { key: 'reply_queue', value: 'postman-replies' },
    { key: 'reply_key', value: 'tss.response.postman' },
  ],
};

const out = process.argv[2] || path.join(__dirname, 'tss-play-gateway.postman_collection.json');
fs.writeFileSync(out, JSON.stringify(collection, null, 2) + '\n');
console.log('wrote', out, 'items:', collection.item.reduce((n, f) => n + (f.item ? f.item.length : 1), 0));
