//Get the location URL information
var appRoot = "/chatter";
var serviceUrl = location.protocol + '//' + location.hostname + (location.port ? ':' + location.port : '');

// Refresh the list of messages every 2 seconds
$(function () {
    setInterval(function () { GetMessages(); }, 2000);
});

$('#sendButton').click(function () {
    AddMessage();
});

$('#deleteButton').click(function () {
    DeleteMessages();
});

//Call the default POST message for
function AddMessage() {
    var name = $('#inputName').val();
    var message = { MessageText: $('#inputMessage').val(), Name: $('#inputName').val() };
    $.ajax({
        url: serviceUrl + appRoot + '/api/chat/',
        type: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify(message)
    })
        .done(function (addMessage) {
            $('#inputMessage').val('');
        });
}


function DeleteMessages() {
    $.ajax({
        url: serviceUrl + appRoot + '/api/chat/',
        type: 'DELETE',
        contentType: 'application/json',
        dataType: 'json'
    })
        .done(function (result) {
            $('#messages').empty();
        });
}

function GetMessages() {
    $.ajax({
        url: serviceUrl + appRoot + '/api/chat/',
        type: 'GET',
        contentType: 'application/json',
        datatype: 'json',
        cache: false
    })
        .done(function (getMessageResult) {
            bindData($('#messages'), getMessageResult);
        });
}

//Take returned messages and construct list items
function bindData(element, data) {
    $('#messages').empty();
    $.each(data, function (id, jobject) {
        $('<li/>')
            .append(
                $('<span class="message-time"/>').text($.time(jobject.key)))
            .append(
                $('<span class="message-from"/>').text(jobject.value.name))
            .append(': ')
            .append(
                 $('<span class="message-body"/>').text(jobject.value.messageText))
            .appendTo(element);
    });
}

//Do formatting of returned datetime
$.time = function (dateObject) {
    var d = new Date(dateObject);
    var sec = d.getSeconds();
    var min = d.getMinutes();
    var hour = d.getHours();
    var time = hour + ":" + min + ":" + sec;
    return time;
};
