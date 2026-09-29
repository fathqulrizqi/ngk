$(document).ready(function () {

    $("#selLCMPPStatusPeriod").change(function () {
        $("#btnLCMPPStatusPeriod").click();
    });
    if ($("#tblLCMPPStatus").length) {
        $("#tblLCMPPStatus").tablesorter({
            theme: 'metro-dark',
            widthFixed: true,
            widgets: ['zebra', 'columns', 'filter', 'stickyHeaders'],
            widgetOptions: {
                stickyHeaders_attachTo: $("#tblLCMPPStatus").parent(),
                stickyHeaders_yScroll: $(window),
                filter_saveFilters: true,
                filter_searchDelay: 750
            }
        }).tablesorterPager({
            container: $(".pager"),
            output: '{startRow} to {endRow} ({totalRows})'
        });
    }
});