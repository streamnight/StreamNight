/* Video below here */

var masterUrl = "/stream/MasterPlaylist";

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
    http.open('GET', window.location.protocol + "//" + window.location.host + masterUrl, true);
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
        isAuto = false;
        document.getElementById('qualitySelect').selectedIndex = player.qualityLevels().selectedIndex_;
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
    option.textContent = "Auto";
    qualitySelect.appendChild(option);

    for (i = 0; i < levelArr.length; i++) {
        var option = document.createElement("option");
        option.setAttribute("value", i.toString());
        option.classList.add("resolutionOption");
        option.textContent = levelArr[i].height.toString() + "p";

        qualitySelect.appendChild(option);
    }

    var controlBar = document.getElementsByClassName("vjs-control-bar")[0];
    controlBar.insertBefore(qualitySelect, controlBar.lastChild);

    qualityExists = true;
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

    player.on('useractive', function () {
        if (isAuto) {
            document.getElementById("qualitySelect")[0].innerText = "Auto: " + player.videoHeight() + "p";
        }
    });

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

    document.getElementById("viewerDetails").classList.add("hidden");
    document.getElementById("settingsPopup").classList.add("hidden");
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