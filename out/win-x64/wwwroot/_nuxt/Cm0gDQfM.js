import{U as e,f as t,k as n,p as r,u as i,z as a}from"./DxfkHh_f.js";import{r as o,t as s}from"./Cxvj1CVF.js";var c=o.extend({name:`card`,style:`
    .p-card {
        background: dt('card.background');
        color: dt('card.color');
        box-shadow: dt('card.shadow');
        border-radius: dt('card.border.radius');
        display: flex;
        flex-direction: column;
    }

    .p-card-caption {
        display: flex;
        flex-direction: column;
        gap: dt('card.caption.gap');
    }

    .p-card-body {
        padding: dt('card.body.padding');
        display: flex;
        flex-direction: column;
        gap: dt('card.body.gap');
    }

    .p-card-title {
        font-size: dt('card.title.font.size');
        font-weight: dt('card.title.font.weight');
    }

    .p-card-subtitle {
        color: dt('card.subtitle.color');
    }
`,classes:{root:`p-card p-component`,header:`p-card-header`,body:`p-card-body`,caption:`p-card-caption`,title:`p-card-title`,subtitle:`p-card-subtitle`,content:`p-card-content`,footer:`p-card-footer`}}),l={name:`Card`,extends:{name:`BaseCard`,extends:s,style:c,provide:function(){return{$pcCard:this,$parentInstance:this}}},inheritAttrs:!1};function u(o,s,c,l,u,d){return a(),r(`div`,n({class:o.cx(`root`)},o.ptmi(`root`)),[o.$slots.header?(a(),r(`div`,n({key:0,class:o.cx(`header`)},o.ptm(`header`)),[e(o.$slots,`header`)],16)):t(``,!0),i(`div`,n({class:o.cx(`body`)},o.ptm(`body`)),[o.$slots.title||o.$slots.subtitle?(a(),r(`div`,n({key:0,class:o.cx(`caption`)},o.ptm(`caption`)),[o.$slots.title?(a(),r(`div`,n({key:0,class:o.cx(`title`)},o.ptm(`title`)),[e(o.$slots,`title`)],16)):t(``,!0),o.$slots.subtitle?(a(),r(`div`,n({key:1,class:o.cx(`subtitle`)},o.ptm(`subtitle`)),[e(o.$slots,`subtitle`)],16)):t(``,!0)],16)):t(``,!0),i(`div`,n({class:o.cx(`content`)},o.ptm(`content`)),[e(o.$slots,`content`)],16),o.$slots.footer?(a(),r(`div`,n({key:1,class:o.cx(`footer`)},o.ptm(`footer`)),[e(o.$slots,`footer`)],16)):t(``,!0)],16)],16)}l.render=u;export{l as default};