$(document).ready(function () {
    if ($("#tbQCCProgress").length) {
        $("#tbQCCProgress").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'QCCUserList.csv',
                output_replaceCR: '&amp;'
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
        });
    }
    $(".selAnggota").change(function () {
        var sectionArray = [];
        $(".selAnggota").each(function (e) {
            var selected = $(this).find('option:selected', this);
            selected.each(function () {
                var currVal = $(this).val();
                if (currVal !== "") {
                    var section = $(this).data("subsection") ? $(this).data("subsection") : $(this).data("sect");
                    if (!section) {
                        section = $(this).data("dept") ? $(this).data("dept") : $(this).data("div");
                    }
                    sectionArray.push(section);
                }
            });
        });

        $("#hfSection").val($.unique(sectionArray.sort()).join(","));
    });
    $('.tablesorter').on("click", ".btnDownload", function () {
        $("#tbQCCProgress").trigger('outputTable');
    });
    $('.MDDatepicker').datepicker({
        changeMonth: true,
        changeYear: true,
        showButtonPanel: true,
        dateFormat: 'M-yy'
    }).focus(function () {
        var thisCalendar = $(this);
        $('.ui-datepicker-calendar').detach();
        $('.ui-datepicker-close').click(function () {
            var month = $("#ui-datepicker-div .ui-datepicker-month :selected").val();
            var year = $("#ui-datepicker-div .ui-datepicker-year :selected").val();
            thisCalendar.datepicker('setDate', new Date(year, month, 1));
        });
    });
    $(".btnDelete").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(this).data("id");
        if (confirm("Are you sure want to delete this data ?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/QCC/Data/deleteList",
                data: { iID: currID },
                success: function (data) {
                    currTR.remove();
                    $("#tbQCCProgress").trigger("update");
                }, error: function () {
                    $("#tbQCCProgress").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
    $(".btnEdit").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(this).data("id");
        //var currMember = currTR.find("td:eq(6)").data("member").split(",");
        var currMember = ["KK.1142", "KK.1143"];
        $("#selPeriod").val(currTR.find("td:eq(0)").text());
        $("#txtGroupName").val(currTR.find("td:eq(1)").text());
        $("#selType").val(currTR.find("td:eq(2)").text()).select2();
        $("#txtTema").val(currTR.find("td:eq(3)").text());
        $("#selFasilitator").val(currTR.find("td:eq(4)").data("nik")).select2();
        $("#selLeader").val(currTR.find("td:eq(5)").data("nik")).select2();
        $("#selAnggota option").filter(function () {
            return !$.inArray($(this).data("nik"), currMember);
        }).prop('selected', true).select2();
        $("#srModal").modal();

    });
    var currStepBtn = null;
    $(".btnStep").click(function () {
        currStepBtn = $(this);
        var currLastStep = $(this).closest("tr").data("step");
        var currStep = $(this).closest("td").data("step");
        var currList = $(this).closest("tr").data("id");
        var currAllowEdit = $(this).closest("tr").data("allowsubmit");
        if (currAllowEdit == true) {
            $(".rowProgressFile,.fileProgress,#btnStepSubmit,#btnStepCancel").removeClass("d-none");
        } else {
            $(".rowProgressFile,.fileProgress,#btnStepSubmit,#btnStepCancel").addClass("d-none");
        }
        $.ajax({
            type: "POST",
            url: "/NGKBusi/QCC/Data/getStep",
            data: {
                iStep: currStep,
                iListID: currList
            },
            success: function (data) {
                var liFiles = "";
                $("#txtNote").val(data.note.trim());
                $(".ulFiles").empty();
                $.each(data.files, function (k, v) {
                    switch (v.ext) {
                        case ".doc":
                        case ".docx":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/QCC/Progress/" + currList + "/" + currStep + "/" + v.filename + "' target='_blank'><i class='fa fa-file-word' style='color: blue; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                        case ".xls":
                        case ".xlsx":
                        case ".csv":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/QCC/Progress/" + currList + "/" + currStep + "/" + v.filename + "' target='_blank'><i class='fa fa-file-excel' style='color: green; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li>";
                            break;
                        case ".pdf":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/QCC/Progress/" + currList + "/" + currStep + "/" + v.filename + "' target='_blank'><i class='fa fa-file-pdf' style='color: red; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                        case ".ppt":
                        case ".pptx":
                            liFiles += "<li style = 'list-style: none; '><a href='" + window.location.origin + "/NGKbusi/files/QCC/Progress/" + currList + "/" + currStep + "/" + v.filename + "' target='_blank'><i class='fa fa-file-powerpoint' style='color: chocolate; '></i> " + v.filename + "</a><i data-id='" + v.id + "' class='ifileDelete fa fa-times ml-2'></i></li >";
                            break;
                    }
                });
                if (currLastStep == currStep) {
                    $("#btnStepCancel").show();
                } else {
                    $("#btnStepCancel").hide();
                }
                $(".ulFiles").append(liFiles);
                if ($(".ulFiles li").length < 1) {
                    liFiles = "<li style = 'list-style: none; '>-</li>";
                    $(".ulFiles").append(liFiles);
                }
            }, error: function () {
                alert("Error Occurred, Please try again !");
            }
        });
        $("#hfListID").val(currList);
        $("#hfStep").val(currStep);
        $("#spanStep").text(currStep);
        $("#qccProgress").modal();
    });
    $('.ulFiles').on('click', '.ifileDelete', function () {
        var currLi = $(this).closest("li");
        var currID = $(this).data("id");
        if (confirm("Are you sure want to delete this file ?")) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/QCC/Data/deleteFile",
                data: {
                    iID: currID
                },
                success: function (data) {
                    currLi.remove();
                }, error: function () {
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
    $("#btnStepCancel").click(function () {
        var currStep = $("#hfStep").val();
        var currListID = $("#hfListID").val();

        if (confirm("Are you sure want to cancel this step ?")) {
            $.ajax({
                type: "POST",
                url: "/NGKBusi/QCC/Data/cancelStep",
                data: {
                    iID: currListID,
                    iStep: currStep
                },
                success: function (data) {
                    currStepBtn.css("color", "lightgrey").toggleClass("fa-check-circle fa-circle");
                    $("#qccProgress").modal("hide");
                    location.reload(true);
                }, error: function () {
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
});