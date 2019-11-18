/* Polyfills for IE */
// Production steps of ECMA-262, Edition 6, 22.1.2.1
if (!Array.from) {
    Array.from = (function () {
        var toStr = Object.prototype.toString;
        var isCallable = function (fn) {
            return typeof fn === 'function' || toStr.call(fn) === '[object Function]';
        };
        var toInteger = function (value) {
            var number = Number(value);
            if (isNaN(number)) { return 0; }
            if (number === 0 || !isFinite(number)) { return number; }
            return (number > 0 ? 1 : -1) * Math.floor(Math.abs(number));
        };
        var maxSafeInteger = Math.pow(2, 53) - 1;
        var toLength = function (value) {
            var len = toInteger(value);
            return Math.min(Math.max(len, 0), maxSafeInteger);
        };

        // The length property of the from method is 1.
        return function from(arrayLike/*, mapFn, thisArg */) {
            // 1. Let C be the this value.
            var C = this;

            // 2. Let items be ToObject(arrayLike).
            var items = Object(arrayLike);

            // 3. ReturnIfAbrupt(items).
            if (arrayLike == null) {
                throw new TypeError('Array.from requires an array-like object - not null or undefined');
            }

            // 4. If mapfn is undefined, then let mapping be false.
            var mapFn = arguments.length > 1 ? arguments[1] : void undefined;
            var T;
            if (typeof mapFn !== 'undefined') {
                // 5. else
                // 5. a If IsCallable(mapfn) is false, throw a TypeError exception.
                if (!isCallable(mapFn)) {
                    throw new TypeError('Array.from: when provided, the second argument must be a function');
                }

                // 5. b. If thisArg was supplied, let T be thisArg; else let T be undefined.
                if (arguments.length > 2) {
                    T = arguments[2];
                }
            }

            // 10. Let lenValue be Get(items, "length").
            // 11. Let len be ToLength(lenValue).
            var len = toLength(items.length);

            // 13. If IsConstructor(C) is true, then
            // 13. a. Let A be the result of calling the [[Construct]] internal method 
            // of C with an argument list containing the single item len.
            // 14. a. Else, Let A be ArrayCreate(len).
            var A = isCallable(C) ? Object(new C(len)) : new Array(len);

            // 16. Let k be 0.
            var k = 0;
            // 17. Repeat, while k < len… (also steps a - h)
            var kValue;
            while (k < len) {
                kValue = items[k];
                if (mapFn) {
                    A[k] = typeof T === 'undefined' ? mapFn(kValue, k) : mapFn.call(T, kValue, k);
                } else {
                    A[k] = kValue;
                }
                k += 1;
            }
            // 18. Let putStatus be Put(A, "length", len, true).
            A.length = len;
            // 20. Return A.
            return A;
        };
    }());
}

// https://tc39.github.io/ecma262/#sec-array.prototype.findindex
if (!Array.prototype.findIndex) {
    Object.defineProperty(Array.prototype, 'findIndex', {
        value: function (predicate) {
            // 1. Let O be ? ToObject(this value).
            if (this == null) {
                throw new TypeError('"this" is null or not defined');
            }

            var o = Object(this);

            // 2. Let len be ? ToLength(? Get(O, "length")).
            var len = o.length >>> 0;

            // 3. If IsCallable(predicate) is false, throw a TypeError exception.
            if (typeof predicate !== 'function') {
                throw new TypeError('predicate must be a function');
            }

            // 4. If thisArg was supplied, let T be thisArg; else let T be undefined.
            var thisArg = arguments[1];

            // 5. Let k be 0.
            var k = 0;

            // 6. Repeat, while k < len
            while (k < len) {
                // a. Let Pk be ! ToString(k).
                // b. Let kValue be ? Get(O, Pk).
                // c. Let testResult be ToBoolean(? Call(predicate, T, « kValue, k, O »)).
                // d. If testResult is true, return k.
                var kValue = o[k];
                if (predicate.call(thisArg, kValue, k, o)) {
                    return k;
                }
                // e. Increase k by 1.
                k++;
            }

            // 7. Return -1.
            return -1;
        },
        configurable: true,
        writable: true
    });
}

/* Video below here */
var masterUrl = "/stream/MasterPlaylist";

if (playlistRedirected === true) {
    masterUrl = redirectedUrl;
}

document.getElementById("playlistSource").innerText = "Playlist Source: " + masterUrl;

var player;
var hlsjsref;
var http;

var lastTypingSent = 0;
var typingUsersDisplayed = new Array();
var removeTypingRunning = false;
var removeTypingId;
var TypingClient;

var isAuto = true;
var isReady = false;
var qualityExists = false;
var isScroll = false;

var lastTap = 0;

function checkStream() {
    http = new XMLHttpRequest();
    if (playlistRedirected) {
        http.open('GET', masterUrl, true);
    }
    else {
        http.open('GET', window.location.protocol + "//" + window.location.host + masterUrl, true);
    }
    http.onload = function () {
        if (http.status == 200) {
            streamUp();
        }
    };
    try {
        http.send();
    }
    catch (e) {
        return false;
    }
}

var removePoster = function () {
    if (!onlinePoster.classList.contains("hidden")) {
        onlinePoster.classList.add("hidden");
    }
}

var onlinePoster = document.getElementById("onlineNotice");
var offlinePoster = document.getElementById("offlineNotice");

var levelLoaded = function () {
    if (isAuto) { }
    else {
        var qualitySelector = document.getElementById('qualitySelect');
        var qualityString = player.qualityLevels().selectedIndex_.toString();
        isAuto = false;
        (Array.from(qualitySelector.options)).forEach(function (item) {
            if (item.value == qualityString) {
                qualitySelector.selectedIndex = item.index;
            }
        });
    }

    tracks = player.textTracks();

    for (let i = 0; i < tracks.length; i++) {
        if (tracks[i].label === 'segment-metadata') {
            segmentMetadataTrack = tracks[i];
        }
    }
}

var qualitySelected = function () {
    var qualitySelector = document.getElementById('qualitySelect');
    if (qualitySelector.selectedIndex == 0) {
        isAuto = true;

        player.qualityLevels().levels_.forEach(function (item) {
            item.enabled = true;
        });
    }
    else {
        isAuto = false;
        player.qualityLevels().selectedIndex_ = parseInt(document.getElementById('qualitySelect')[document.getElementById('qualitySelect').selectedIndex].value, 10);

        var i = 0;
        player.qualityLevels().levels_.forEach(function (item) {
            if (i == player.qualityLevels().selectedIndex_) {
                item.enabled = true;
            }
            else {
                item.enabled = false;
            }
            i++;
        });
    }

    player.qualityLevels().trigger({ type: 'change', selectedIndex: parseInt(document.getElementById('qualitySelect')[document.getElementById('qualitySelect').selectedIndex].value, 10) });
}

function buildQualitySelectNative() {
    var levelArr = player.qualityLevels().levels_;
    var playlistArr = player.vhs.playlists.master.playlists;
    var initialPlaylist = player.vhs.masterPlaylistController_.initialMedia_.attributes;

    var qualitySelect;

    if (qualityExists) {
        qualitySelect = document.getElementById("qualitySelect");
        qualitySelect.innerHTML = '';
    }
    else {
        qualitySelect = document.createElement("select");
    }
    qualitySelect.classList.add("vjs-control");
    qualitySelect.setAttribute("onchange", "qualitySelected()");
    qualitySelect.id = "qualitySelect";

    var option = document.createElement("option");
    option.setAttribute("value", -1);
    option.classList.add("resolutionOption");
    if (initialPlaylist["FRAME-RATE"] != null && initialPlaylist["RESOLUTION"].height != null) {
        option.textContent = "Auto: " + initialPlaylist["RESOLUTION"].height + "/" + Math.round(initialPlaylist["FRAME-RATE"]);
    }
    else {
        option.textContent = "Auto";
    }
    qualitySelect.appendChild(option);

    for (i = 0; i < levelArr.length; i++) {
        var option = document.createElement("option");
        option.setAttribute("value", i.toString());
        option.classList.add("resolutionOption");
        if (playlistArr[i].attributes["FRAME-RATE"] != null) {
            option.textContent = levelArr[i].height.toString() + "/" + Math.round(playlistArr[i].attributes["FRAME-RATE"]);
        }
        else {
            option.textContent = levelArr[i].height.toString() + "p";
        }

        qualitySelect.appendChild(option);
    }

    var controlBar = document.getElementsByClassName("vjs-control-bar")[0];
    controlBar.insertBefore(qualitySelect, controlBar.lastChild);

    qualityExists = true;

    player.vhs.playlists.on('mediachange', updateAutoQuality);
    player.on('useractive', updateAutoQuality);
}

function updateAutoQuality() {
    if (isAuto && player.vhs.playlists.media_.attributes["FRAME-RATE"] != null) {
        document.getElementById("qualitySelect")[0].innerText = "Auto: " + player.vhs.playlists.media_.attributes["RESOLUTION"].height + "/" + Math.round(player.vhs.playlists.media_.attributes["FRAME-RATE"]);
    }
    else if (isAuto) {
        document.getElementById("qualitySelect")[0].innerText = "Auto: " + player.vhs.playlists.media_.attributes["RESOLUTION"].height + "p";
    }
}

var engine;

function initialiseVideo() {
    player = videojs("streamPlayer", {
        html5: {
            hls: {
                overrideNative: true,
                smoothQualityChange: false
            }
        },
        liveui: true
    });

    document.getElementsByClassName("vjs-big-play-button")[0].addEventListener("click", function () {
        if (!onlinePoster.classList.contains("hidden")) {
            onlinePoster.classList.add("hidden");
        }
    }, false);

    registerTap(document.getElementsByClassName("vjs-big-play-button")[0], removePoster);

    document.getElementById("streamPlayer").classList.remove("hidden");

    player.src({
        src: masterUrl,
        type: "application/x-mpegURL"
    });

    player.qualityLevels().on('change', levelLoaded);
    player.on('loadedmetadata', buildQualitySelectNative);

    isReady = true;

    setTimeout(pollUsers, 2000);
}

checkStream();

function streamUp() {
    onlinePoster.classList.remove("hidden");
    offlinePoster.classList.add("hidden");
    initialiseVideo();
}

function rebuildVideo() {
    if (isReady) {
        isReady = false;
        videojs("streamPlayer").dispose();

        var streamPlayer = document.createElement("video");
        streamPlayer.id = "streamPlayer";
        streamPlayer.setAttribute("controls", "");
        streamPlayer.classList.add("video-js", "vjs-default-skin", "vjs-fluid", "stream-child", "hidden");

        document.getElementById("streamContainer").appendChild(streamPlayer);

        initialiseVideo();
    }
}

function sendTyping() {
    if (Date.now() - lastTypingSent > 5000) {
        connection.invoke("ClientTyping");
        lastTypingSent = Date.now();
    }
}

var removeTyping = function () {
    for (var i = 0; i < typingUsersDisplayed.length; i++) {
        var typingUser = typingUsersDisplayed[i];
        if (parseInt(typingUser.timestamp, 10) + 7500 < Date.now()) {
            var typingUserElements = Array.from(document.getElementsByClassName(typingUser.userId));
            typingUserElements.forEach(function (typingUserElement) {
                typingUserElement.classList.remove("typing");
            });
            typingUsersDisplayed.splice(i, 1);
        }
    }

    if (typingUsersDisplayed.length <= 0) {
        clearInterval(removeTypingId);
        removeTypingRunning = false;
    }
}

connection.on("connectedIds", function (Ids) {
    document.getElementById("viewerCount").innerText = Ids.length + " In Chat";
    var viewerDetails = document.getElementById("viewerDetails");
    viewerDetails.innerHTML = "";

    Ids.forEach(function (Id) {
        if (Id.hasDiscordInfo == true) {
            var viewerData = document.createElement("div");
            var viewerAvatar = document.createElement("img");
            var viewerNameDiv = document.createElement("div");
            var viewerName = document.createElement("p");

            viewerAvatar.setAttribute("src", Id.avatarUrl);
            viewerAvatar.setAttribute("alt", Id.discordDisplayName);
            viewerName.innerText = Id.discordDisplayName;

            viewerData.appendChild(viewerAvatar);
            viewerNameDiv.appendChild(viewerName);
            viewerData.appendChild(viewerNameDiv);
            viewerData.classList.add(Id.discordId);

            var typingUserElementIndex = typingUsersDisplayed.findIndex(function (element) {
                return element.userId == Id.discordId;
            });
            if (typingUserElementIndex > -1) {
                viewerData.classList.add("typing");
            }

            viewerData.classList.add("popupEntry");

            viewerDetails.appendChild(viewerData);
        }
    });
});

connection.on("clientTyping", function (typingClient) {
    TypingClient = typingClient;
    var typingUserElements = Array.from(document.getElementsByClassName(TypingClient.userId));
    typingUserElements.forEach(function (typingUserElement) {
        if (!typingUserElement.classList.contains("typing")) {
            typingUserElement.classList.add("typing");
            typingUsersDisplayed.push(TypingClient);
        }
        else {
            var typingUserElementIndex = typingUsersDisplayed.findIndex(function (element) {
                return element.userId == TypingClient.userId;
            });

            typingUsersDisplayed[typingUserElementIndex].timestamp = Date.now();
        }
    });

    if (!removeTypingRunning) {
        removeTypingId = setInterval(removeTyping, 1000);
        removeTypingRunning = true;
    }
});

connection.on("stopTypingForClient", function (stoppedClient) {
    var typingUserElements = Array.from(document.getElementsByClassName(stoppedClient.discordId));
    typingUserElements.forEach(function (typingUserElement) {
        typingUserElement.classList.remove("typing");
    });

    var userInArray = typingUsersDisplayed.findIndex(function (element) {
        return element.userId == stoppedClient.discordId;
    });
    if (userInArray > -1) {
        typingUsersDisplayed.splice(userInArray, 1);
    }
});

var minPollSpacing = 5000;
var lastPollTime = 0;
var isPolling = false;

connection.on("viewerConnected", function () {
    pollUsers();
});
connection.on("viewerDisconnected", function () {
    pollUsers();
});

function pollUsers() {
    if (!isPolling) {
        isPolling = true;
        setTimeout("connection.invoke('GetConnectedUsers')", Math.max(0, lastPollTime - Date.now() + minPollSpacing));
        lastPollTime = Date.now();
        isPolling = false;
    }
}

connection.on("streamUp", function () {
    if (!isReady) {
        onlinePoster.classList.remove("hidden");
        offlinePoster.classList.add("hidden");
        initialiseVideo();
    }
});


/* Chat below here */
var emojiPicker = document.getElementById("emoji-picker");
var emojiHiders = Array.from(document.getElementsByClassName("emojiDeselect"));
var emoteButton = document.getElementById("emoteButton");

var hiderClick = function () {
    if (!emojiPicker.classList.contains("softHidden")) {
        emojiPicker.setAttribute("aria-hidden", true);
        emojiPicker.classList.add("softHidden");
    }

    try {
        document.getElementById("viewerData").attributes.removeNamedItem("open");
    }
    catch (e) { }
    try {
        document.getElementById("settings").attributes.removeNamedItem("open");
    }
    catch (e) { }

    emojiHiders.forEach(function (emojiHider) {
        emojiHider.classList.toggle("hidden");
    });}

var pickerClick = function () {
    emojiPicker.setAttribute("aria-hidden", false);
    emojiPicker.classList.toggle("softHidden");
    emojiHiders.forEach(function (emojiHider) {
        emojiHider.classList.toggle("hidden");
    });}

var hideOnClick = function () {
    if (Date.now() - lastTap > 100) {
        emojiHiders.forEach(function (emojiHider) {
            emojiHider.classList.toggle("hidden");
        });
    }
}

emojiHiders.forEach(function (emojiHider) {
    emojiHider.addEventListener("click", hiderClick);
    registerTap(emojiHider, hiderClick);
});

emoteButton.addEventListener("click", pickerClick);
registerTap(emoteButton, pickerClick);

document.getElementById("viewerCount").addEventListener("click", pollUsers);
registerTap(document.getElementById("viewerCount"), pollUsers);

if (detectIE() != false) {
    console.warn("User is running Internet Explorer.");
    connection.invoke("NotifyOldBrowser").catch (function (err) {
        return console.error(err.toString());
    });
    emoteButton.classList.add("hidden");

    var oldNotif = document.createElement("li");
    var oldNotifText = document.createElement("p");
    oldNotifText.innerText = "We don't support emotes under Internet Explorer or EdgeHTML-based Microsoft Edge. Consider upgrading to Mozilla Firefox or Google Chrome.";
    oldNotifText.setAttribute("style", "color:orange;");
    oldNotif.appendChild(oldNotifText);
    document.getElementById("chatbox").appendChild(oldNotif);

    document.getElementById("settingsPopup").classList.add("softHidden");


    document.getElementById("settings").addEventListener("click", function () {
        document.getElementById("settingsPopup").classList.toggle('softHidden');
    }, false);
}

function toggleChat() {
    var chat = document.getElementById("chat");
    var body = document.body;
    document.getElementById("settings").removeAttribute("open");

    if (matchMedia('(orientation:portrait)').matches) {
        body.classList.remove("moveChat");
        chat.classList.remove("softHidden");
        if (body.classList.contains("hideChat")) {
            document.getElementById("video").classList.remove("fullscreen");
            chat.classList.remove("hidden");
            setTimeout(function () {
                body.classList.remove("hideChat");
            }, 100);
        }
        else {
            document.getElementById("video").classList.add("fullscreen");
            body.classList.add("hideChat");
            setTimeout(function () {
                chat.classList.add("hidden");
            }, 300);
        }
    }
    else {
        document.getElementById("video").classList.remove("fullscreen");
        if (body.classList.contains("hideChat")) {
            body.classList.remove("hideChat");
            chat.classList.remove("hidden");
            setTimeout(function () {
                chat.classList.remove("softHidden");
                setTimeout(function () {
                    body.classList.remove("moveChat");
                }, 1);
            }, 1);
        }
        else {
            document.getElementById("video").classList.add("fullscreen");
            body.classList.add("moveChat");
            setTimeout(function () {
                body.classList.add("hideChat");
                chat.classList.add("softHidden");
                chat.classList.add("hidden");
            }, 300);
        }
    }
}

function toggleViewers() {
    if (document.body.classList.contains("hideViewers")) {
        document.getElementById("viewerDetails").classList.remove('softHidden');
        setTimeout(function () {
            document.body.classList.remove("hideViewers");
        }, 10);
    }
    else {
        document.body.classList.add("hideViewers");
        setTimeout(function () {
            document.getElementById("viewerDetails").classList.add('softHidden');
        }, 5);
    }
}