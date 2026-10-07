import{U as e,k as t,p as n,z as r}from"./DxfkHh_f.js";import{r as i,t as a}from"./Cxvj1CVF.js";var o=i.extend({name:`buttongroup`,style:`
    .p-buttongroup {
        display: inline-flex;
    }

    .p-buttongroup .p-button {
        margin: 0;
    }

    .p-buttongroup .p-button:not(:last-child),
    .p-buttongroup .p-button:not(:last-child):hover {
        border-inline-end: 0 none;
    }

    .p-buttongroup .p-button:not(:first-of-type):not(:last-of-type) {
        border-radius: 0;
    }

    .p-buttongroup .p-button:first-of-type:not(:only-of-type) {
        border-start-end-radius: 0;
        border-end-end-radius: 0;
    }

    .p-buttongroup .p-button:last-of-type:not(:only-of-type) {
        border-start-start-radius: 0;
        border-end-start-radius: 0;
    }

    .p-buttongroup .p-button:focus {
        position: relative;
        z-index: 1;
    }
`,classes:{root:`p-buttongroup p-component`}}),s={name:`ButtonGroup`,extends:{name:`BaseButtonGroup`,extends:a,style:o,provide:function(){return{$pcButtonGroup:this,$parentInstance:this}}},inheritAttrs:!1};function c(i,a,o,s,c,l){return r(),n(`span`,t({class:i.cx(`root`),role:`group`},i.ptmi(`root`)),[e(i.$slots,`default`)],16)}s.render=c;export{s as default};