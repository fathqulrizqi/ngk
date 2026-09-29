$(function () {
    $('.js-sweetalert button').on('click', function () {
        var type = $(this).data('type');
        if (type === 'basic') {
            showBasicMessage();
        }
        else if (type === 'failed') {
            showFailedMessage();
        }
        else if (type === 'with-title') {
            showWithTitleMessage();
        }
        else if (type === 'success') {
            showSuccessMessage();
        }
        else if (type === 'success') {
            showSuccessMessageReload();
        }
        else if (type === 'confirm') {
            showConfirmMessage();
        }
        else if (type === 'confirm') {
            showConfirmSendRequest();
        }
        else if (type === 'confirm') {
            showConfirmActivateMessage();
        }
        else if (type === 'cancel') {
            showCancelMessage();
        }
        else if (type === 'with-custom-icon') {
            showWithCustomIconMessage();
        }
        else if (type === 'html-message') {
            showHtmlMessage();
        }
        else if (type === 'autoclose-timer') {
            showAutoCloseTimerMessage();
        }
        else if (type === 'prompt') {
            showPromptMessage();
        }
        else if (type === 'ajax-loader') {
            showAjaxLoaderMessage();
        }
    });
});

//These codes takes from http://t4t5.github.io/sweetalert/
function showBasicMessage(message) {
    swal(message);
}

function showFailedMessage(message) {
   swal("Failed", message, "error");
}

function showWithTitleMessage() {
    swal("Here's a message!", "It's pretty, isn't it?");
}

function showSuccessMessage(message) {
    swal(message, "", "success");
}

function showSuccessMessageReload(message) {
    swal({title: "Good job", text: message, type: "success"},
       function(){ 
           location.reload();
       }
    );
}

function showConfirmMessage(Link, Message) {
    swal({
        title: "Are you sure?",
        text: "Delete This Data!",
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#DD6B55",
        confirmButtonText: "Yes, delete it!",
        closeOnConfirm: false,
        closeOnCancel: false
    }, function (isConfirm) {
            if (isConfirm) {
                $.ajax({
                    url: Link,
                    type: "POST",
                    cache: false,
                    dataType:'json',
                    success: function(json){
                        if (json.status == 1) {
                            swal("Deleted!", "Your imaginary file has been deleted.", "success");
                            
                        } else {
                            swal("Deleted!", "Your imaginary file has been deleted.", "error");
                        }
                    },
                    error: function(){
                        swal("Deleted!", Link, "error");
                    }
                    
                })
            } else {
              swal("Cancelled", "Your imaginary file is safe :)", "error");
            }
    });
}

function showConfirmSendRequest(Link) {
    swal({
        title: "Are you sure?",
        text: "Send Request Data",
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#5cb85c",
        confirmButtonText: "Yes, Send it!",
        closeOnConfirm: false,
        closeOnCancel: false
    }, function (isConfirm) {
            if (isConfirm) {
                $.ajax({
                    url: Link,
                    type: "POST",
                    cache: false,
                    dataType: 'json',
                    data: $('#formHeaderRequest').serialize(),
                    success: function(json){
                        if (json.status == 1) {
                            
                            swal("Success!", "Your Item Request has been send", "success");
                           
                            
                        } else {
                            swal("Failed!", "failed to send request.", "error");
                        }
                    },
                    error: function(){
                        swal("Deactivate!", Link, "error");
                    }
                    
                })
            } else {
              swal("Cancelled", "Request Not Send", "error");
            }
    });
}

function showConfirmActivateMessage(Link) {
    swal({
        title: "Are you sure?",
        text: "Activate This Data!",
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#DD6B55",
        confirmButtonText: "Yes, Activate it!",
        closeOnConfirm: false,
        closeOnCancel: false
    }, function (isConfirm) {
            if (isConfirm) {
                $.ajax({
                    url: Link,
                    type: "POST",
                    cache: false,
                    dataType:'json',
                    success: function(json){
                        if (json.status == 1) {
                            $('#my-grid').DataTable().ajax.reload( null, false );
                            $('#yy-grid').DataTable().ajax.reload( null, false );
                            swal("Activate!", "Your imaginary file has been deleted.", "success");
                            
                        } else {
                            swal("Activate!", "Your imaginary file has been deleted.", "error");
                        }
                    },
                    error: function(){
                        swal("Activate!", Link, "error");
                    }
                    
                })
            } else {
              swal("Cancelled", "Your imaginary file is safe :)", "error");
            }
    });
}

function showCancelMessage() {
    swal({
        title: "Are you sure?",
        text: "You will not be able to recover this imaginary file!",
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#DD6B55",
        confirmButtonText: "Yes, delete it!",
        cancelButtonText: "No, cancel plx!",
        closeOnConfirm: false,
        closeOnCancel: false
    }, function (isConfirm) {
        if (isConfirm) {
            swal("Deleted!", "Your imaginary file has been deleted.", "success");
        } else {
            swal("Cancelled", "Your imaginary file is safe :)", "error");
        }
    });
}

function showWithCustomIconMessage() {
    swal({
        title: "Sweet!",
        text: "Here's a custom image.",
        imageUrl: "../../images/thumbs-up.png"
    });
}

function showHtmlMessage() {
    swal({
        title: "HTML <small>Title</small>!",
        text: "A custom <span style=\"color: #CC0000\">html<span> message.",
        html: true
    });
}

function showAutoCloseTimerMessage() {
    swal({
        title: "Auto close alert!",
        text: "I will close in 2 seconds.",
        timer: 2000,
        showConfirmButton: false
    });
}

function showPromptMessage() {
    swal({
        title: "An input!",
        text: "Write something interesting:",
        type: "input",
        showCancelButton: true,
        closeOnConfirm: false,
        animation: "slide-from-top",
        inputPlaceholder: "Write something"
    }, function (inputValue) {
        if (inputValue === false) return false;
        if (inputValue === "") {
            swal.showInputError("You need to write something!"); return false
        }
        swal("Nice!", "You wrote: " + inputValue, "success");
    });
}

function showAjaxLoaderMessage() {
    swal({
        title: "Ajax request example",
        text: "Submit to run ajax request",
        type: "info",
        showCancelButton: true,
        closeOnConfirm: false,
        showLoaderOnConfirm: true,
    }, function () {
        setTimeout(function () {
            swal("Ajax request finished!");
        }, 2000);
    });
}