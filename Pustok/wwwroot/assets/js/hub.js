var connection = new signalR.HubConnectionBuilder().withUrl("/apphub").build();

connection.start();


connection.on("Connected", function (id) {
    if ($("#userTable")) {
        
        console.log("userId:" + id);
        let span = $("#userTable").find("[data-id='" + id + "'] span");
        $(span).removeClass("dot-offline");
        $(span).addClass("dot-online");
    }
})

connection.on("Disconnected", function (id) {
    if ($("#userTable")) {
        console.log("userId:" + id);
        let span = $("#userTable").find("[data-id='" + id + "'] span");
        $(span).removeClass("dot-online");
        $(span).addClass("dot-offline");
    }
})

connection.on("OrderAccepted", function () {
    toastr["success"]("Sifarisiniz tesdiqlendi!")
})





