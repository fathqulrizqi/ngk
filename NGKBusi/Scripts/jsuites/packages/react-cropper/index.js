// @ts-nocheck
import React, { useRef, useEffect } from "react";
import cropper from "@jsuites/cropper";

// @ts-ignore
export default React.forwardRef((props, mainReference) => {
    // Dom element
    const Ref = useRef(null);

    // Get the properties for the spreadsheet
    let options = { ...props };

    useEffect(() => {
        // @ts-ignore
        if (!Ref.current.innerHTML) {
            mainReference.current = cropper(Ref.current, options);
        }
    }, []);

    let prop = {
        ref: Ref,
        style: props.style || {}
    };

    return React.createElement("div", prop);
})