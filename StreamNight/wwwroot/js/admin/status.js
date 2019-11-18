var statusTable = document.getElementById("status-table");
var statusTableBody = statusTable.getElementsByTagName('tbody')[0];

var connectionStatus = document.getElementById("connection-status");
var connectionAttemptsElement = document.getElementById("connection-attempts");
var connectionAttempts = 1;

var connection = new signalR.HubConnectionBuilder().withUrl("/admin/statushub").build();
connection.serverTimeoutInMilliseconds = 10000;

connection.on("systemStatus", function (SystemStatus) {
    console.log(SystemStatus);

    statusTableBody.innerHTML = '';
    for (var statusProperty in SystemStatus) {
        let propertyRow = statusTableBody.insertRow(-1);

        let propertyName = propertyRow.insertCell(-1);
        propertyName.appendChild(document.createTextNode(FixPropertyName(statusProperty)));

        let propertyValue = propertyRow.insertCell(-1);
        propertyValue.appendChild(document.createTextNode(SystemStatus[statusProperty]));

        if (statusProperty == 'LastPoll' || statusProperty == 'LastChange') {
            propertyValue.innerHTML = '';
            propertyValue.appendChild(document.createTextNode(GetTimeFromUnix(SystemStatus[statusProperty])))
        }

        if (propertyValue.innerText == "true") {
            propertyValue.innerText = "Yes";
        }
        else if (propertyValue.innerText == "false") {
            propertyValue.innerText = "No";
        }
    }
});

connection.on("failed", function () {
    alert("The requested operation didn't complete successfully. Make sure the system state is valid and try again.");
});

// https://stackoverflow.com/a/7888303/
function FixPropertyName(string) {
    string = string.replace(/([a-z])([A-Z])/g, '$1 $2');
    string = string.replace("And", "and");

    return string.charAt(0).toUpperCase() + string.slice(1);
}

// Alternative: https://stackoverflow.com/a/35890537/
function GetTimeFromUnix(unixTime) {
    return new Date(unixTime * 1000).toLocaleString();
}

connection.onclose(disconnected);

function disconnected() {
    connectionStatus.innerText = "Data may be outdated. Attempting to reconnect to the status page...";
    connectionAttemptsElement.classList.remove("hidden");
    connect();
}

function connect() {
    connectionAttempts++;
    connection.start().then(connected).catch(function (err) {
        connectionAttemptsElement.innerText = "Connection attempts: " + connectionAttempts;
        console.error(err.toString());
        setTimeout(connect, 3000);
    });
}

function connected() {
    connection.invoke("GetSystemStatus");
    connectionAttempts = 0;
    connectionStatus.innerText = "Connected to the status page. Changes are automatically pushed to your browser.";
    connectionAttemptsElement.classList.add("hidden");
}

function stopBot() {
    connection.invoke("StopBot");
    alert("Stopping bot.");
}

function startBot() {
    connection.invoke("StartBot");
    alert("Starting bot.");
}

function toggleServerIcon() {
    connection.invoke("ToggleServerIcon");
    alert("Toggled server icon.");
}

function togglePresence() {
    connection.invoke("TogglePresence");
    alert("Toggled presence.");
}

function togglePlaylistRedirect() {
    connection.invoke("TogglePlaylistRedirect");
    alert("Toggled playlist redirect.");
}

function updatePlaylistUrl() {
    connection.invoke("SetRedirectTarget", document.getElementById("redirect-url").value);
    document.getElementById("redirect-url").value = "";
    alert("Updated redirect.");
}

connect();
