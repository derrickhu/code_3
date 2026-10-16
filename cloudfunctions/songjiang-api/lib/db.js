const tcb = require('@cloudbase/node-sdk');
const { getCollectionName } = require('./config');

let app = null;

function getApp() {
  if (app) return app;
  app = tcb.init({
    env: process.env.TCB_ENV || tcb.SYMBOL_CURRENT_ENV,
  });
  return app;
}

function getCollection() {
  return getApp().database().collection(getCollectionName('playerData'));
}

module.exports = {
  getApp,
  getCollection,
};
