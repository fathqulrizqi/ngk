$(document).ready(function () {
    if ($("#tbMachineDB").length) {
        $("#tbMachineDB").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'filter', 'stickyHeaders', 'output'],
            widgetOptions: {
                output_delivery: 'download',
                output_saveFileName: 'MachineDatabase.csv',
                output_replaceCR: '&amp;'
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})',
            size: 50
            });
        $("#tbMachineDB").trigger('update');
    }
    $('.tablesorter').on("click", ".btnDownload", function () {
        $("#tbMachineDB").trigger('outputTable');
    });
    $('.tablesorter').on('click', '.toggle', function () {
        toggleChild($(this));
        return false;
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

    $(".deleteMDItem").click(function () {
        var currTR = $(this).closest("tr");
        var currID = $(this).data("id");
        var currRelID = $(this).data("relID");
        if (confirm("Are you sure want to delete this data?")) {
            currTR.find("td").css("background-color", "orange");
            $.ajax({
                type: "POST",
                url: "/NGKBusi/MTN/MachineDatabase/MDDelete",
                data: { iID: currID },
                success: function (data) {
                    location.reload();
                    $("#tbMachineDB").trigger("update");
                }, error: function () {
                    $("#tbMachineDB").trigger("update");
                    alert("Error Occurred, Please try again !");
                }
            });
        }
    });
    $(".pointer").click(function () {
        $("#selMDArea").show();
        $("#divMDReplace").hide();
        $("#spanMDArea").text("-").hide();
        $("#mdModal form")[0].reset();
        $("#mdModal .modal-title").text("Add New Machine");
        $("#mdModal form").attr("action", "/NGKBusi/MTN/MachineDatabase/MDAdd");
    });
    $(".addMDChild").click(function () {
        var currID = $(this).data("id");
        var currAreaID = $(this).data("areaid");
        var currArea = $(this).data("area");
        $("#mdModal .modal-title").text("Add New Machine");
        $("#mdModal form")[0].reset();
        $("#mdModal form").attr("action","/NGKBusi/MTN/MachineDatabase/MDAdd");
        $("#txtMachineParent").val(currID);
        $("#selMDArea").val(currAreaID);
        $("#selMDArea").hide();
        $("#divMDReplace").show();
        $("#spanMDArea").text(currArea).show();

        $("#mdModal").modal();
    });

    $(".editMDItem").click(function () {
        var currID = $(this).data("id");
        var currTR = $(this).closest("tr");
        var currAreaID = $(this).data("areaid");
        $("#mdModal .modal-title").text("Edit Machine");
        $("#selMDArea").show();
        $("#divMDReplace").hide();
        $("#spanMDArea").text("-").hide();
        $("#txtMachineID").val(currID);
        $("#selMDArea").val(currAreaID);
        $("#txtMachine").val(currTR.find("td:eq(0)").text().trim());
        $("#txtMDMachineNo").val(currTR.find("td:eq(1)").text());
        $("#txtMDAssetNo").val(currTR.find("td:eq(2)").text());
        $("#txtMDModel").val(currTR.find("td:eq(3)").text());
        $("#txtMDPower").val(currTR.find("td:eq(4)").text());
        $("#txtMDMaker").val(currTR.find("td:eq(5)").text());
        $("#txtMDSerial").val(currTR.find("td:eq(6)").text());
        $("#txtMDComingDate").val(currTR.find("td:eq(7)").text());
        $("#txtMDQty").val(currTR.find("td:eq(8)").text());
        $("#txtMDStartDate").val(currTR.find("td:eq(9)").text());
        $("#txtMDEndDate").val(currTR.find("td:eq(10)").text());
        $("#txtMDOverhaulSchedule").val(currTR.find("td:eq(11)").text());
        $("#txtMDRemark").val(currTR.find("td:eq(12)").text());
        $("#selMDStatus").val(currTR.find("td:eq(13)").text());
        $("#cbIsScheduled").prop("checked", currTR.find("td:eq(15)").data("isscheduled") =="True" ? true:false);
        //$("#selMDStatus").val(currTR.find("td:eq(13)").text());
        $("#mdModal form").attr("action", "/NGKBusi/MTN/MachineDatabase/MDEdit");
        $("#mdModal").modal();
    });

    $("#cbMDReplace").change(function () {
        var machineParent = $("#txtMachineParent").val();
        var machineOld = $("#txtMachineOld").val();
        if ($(this).is(":checked")) {
            $("#txtMachineParent").val("");
            $("#txtMachineOld").val(machineParent);
        } else {
            $("#txtMachineOld").val("");
            $("#txtMachineParent").val(machineOld);
        }
    });

    function toggleChild(ctr) {
        var currText = ctr.data("id");
        if ($("." + currText).length) {
            $("." + currText).each(function () {
                $(this).find('td').toggle();
                if ($(this).find(".toggle").length) {
                    var childText = $(this).find(".toggle").data("id");
                    if ($("." + childText).length) {
                        toggleChild($(this).find(".toggle"));
                    }
                }
            });
        }
    }
});